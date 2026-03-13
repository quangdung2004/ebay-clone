using AutoMapper;
using CloneEbay.Application.Products;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Products;

public sealed class ProductService : IProductService
{
    private readonly CloneEbayDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IMapper _mapper;

    public ProductService(CloneEbayDbContext db, IWebHostEnvironment env, IMapper mapper)
    {
        _db = db;
        _env = env;
        _mapper = mapper;
    }

    public async Task<PagedResponse<ProductListItemDto>> GetProductsAsync(
        string? q,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        bool? isAuction,
        string? status,
        string? condition,
        bool? inStock,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Product
            .AsNoTracking()
            .Include(p => p.Inventory)
            .Include(p => p.Bid)
            .Where(p => p.isDeleted != true)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(p =>
                (p.title ?? "").Contains(keyword) ||
                (p.description ?? "").Contains(keyword));
        }

        if (categoryId.HasValue)
            query = query.Where(p => p.categoryId == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(p => (p.price ?? 0) >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => (p.price ?? 0) <= maxPrice.Value);

        if (isAuction.HasValue)
            query = query.Where(p => p.isAuction == isAuction.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(p => p.status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(condition))
        {
            var normalizedCondition = condition.Trim().ToUpperInvariant();
            query = query.Where(p => p.condition == normalizedCondition);
        }

        if (inStock.HasValue)
        {
            if (inStock.Value)
                query = query.Where(p => p.Inventory.Any(i => (i.quantity ?? 0) > 0));
            else
                query = query.Where(p => !p.Inventory.Any(i => (i.quantity ?? 0) > 0));
        }

        query = (sort ?? "newest").ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.price),
            "price_desc" => query.OrderByDescending(p => p.price),
            "oldest" => query.OrderBy(p => p.id),
            "ending_soon" => query.OrderBy(p => p.auctionEndTime ?? DateTime.MaxValue),
            "most_viewed" => query.OrderByDescending(p => p.viewCount ?? 0),
            _ => query.OrderByDescending(p => p.id)
        };

        var total = await query.CountAsync(ct);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        foreach (var p in rows)
        {
            if (p.isAuction == true && p.status == ProductStatuses.Active && p.auctionEndTime.HasValue && p.auctionEndTime.Value <= DateTime.UtcNow)
            {
                p.status = ProductStatuses.Ended;
            }
        }

        var items = _mapper.Map<List<ProductListItemDto>>(rows);
        return new PagedResponse<ProductListItemDto>(items, page, pageSize, total);
    }

    public async Task<ProductDetailDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var exists = await _db.Product
            .AsNoTracking()
            .AnyAsync(x => x.id == id && x.isDeleted != true, ct);

        if (!exists)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        await IncreaseViewCountAsync(id, ct);

        var p = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .Include(x => x.category)
            .Include(x => x.seller)
            .AsNoTracking()
            .FirstAsync(x => x.id == id && x.isDeleted != true, ct);

        if (p.isAuction == true && p.status == ProductStatuses.Active && p.auctionEndTime.HasValue && p.auctionEndTime.Value <= DateTime.UtcNow)
        {
            p.status = ProductStatuses.Ended;
        }

        return _mapper.Map<ProductDetailDto>(p);
    }

    public async Task<ProductDetailDto> CreateAsync(int sellerId, CreateProductRequest req, CancellationToken ct)
    {
        ValidateCreate(req);

        var hasStore = await _db.Store
            .AsNoTracking()
            .AnyAsync(x => x.sellerId == sellerId, ct);

        if (!hasStore)
            throw new ForbiddenException(
                "You must create a store before selling products",
                "STORE_REQUIRED");

        var catExists = await _db.Category.AnyAsync(c => c.id == req.categoryId, ct);
        if (!catExists)
            throw new NotFoundException("Category not found", "CATEGORY_NOT_FOUND");

        var p = new Product
        {
            title = req.title.Trim(),
            description = req.description?.Trim(),
            price = req.price,
            categoryId = req.categoryId,
            sellerId = sellerId,
            isAuction = req.isAuction,
            auctionEndTime = req.isAuction ? req.auctionEndTime : null,
            status = DetermineStatus(req.isAuction, req.auctionEndTime, req.quantity),
            condition = NormalizeCondition(req.condition),
            viewCount = 0,
            isDeleted = false,
            deletedAt = null
        };

        ProductImageJson.Write(p, Array.Empty<string>());

        _db.Product.Add(p);
        await _db.SaveChangesAsync(ct);

        _db.Inventory.Add(new Inventory
        {
            productId = p.id,
            quantity = req.quantity,
            lastUpdated = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var created = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .AsNoTracking()
            .FirstAsync(x => x.id == p.id, ct);

        return _mapper.Map<ProductDetailDto>(created);
    }

    public async Task<ProductDetailDto> UpdateAsync(int sellerId, int productId, UpdateProductRequest req, CancellationToken ct)
    {
        ValidateUpdate(req);

        var p = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        var catExists = await _db.Category.AnyAsync(c => c.id == req.categoryId, ct);
        if (!catExists)
            throw new NotFoundException("Category not found", "CATEGORY_NOT_FOUND");

        var hasBids = p.Bid.Any();

        if (hasBids)
        {
            if (p.isAuction != req.isAuction)
                throw new ValidationException("Cannot change auction type after bids exist", "AUCTION_HAS_BIDS");

            if (p.price != req.price)
                throw new ValidationException("Cannot change starting price after bids exist", "AUCTION_HAS_BIDS");

            if (p.auctionEndTime != req.auctionEndTime)
                throw new ValidationException("Cannot change auction end time after bids exist", "AUCTION_HAS_BIDS");
        }

        p.title = req.title.Trim();
        p.description = req.description?.Trim();
        p.price = req.price;
        p.categoryId = req.categoryId;
        p.isAuction = req.isAuction;
        p.auctionEndTime = req.isAuction ? req.auctionEndTime : null;
        p.condition = NormalizeCondition(req.condition);

        var quantity = p.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault();
        p.status = DetermineStatus(req.isAuction, req.auctionEndTime, quantity, p.status);

        await _db.SaveChangesAsync(ct);

        var updated = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .AsNoTracking()
            .FirstAsync(x => x.id == productId, ct);

        return _mapper.Map<ProductDetailDto>(updated);
    }

    public async Task UpdateInventoryAsync(int sellerId, int productId, int quantity, CancellationToken ct)
    {
        if (quantity < 0)
            throw new ValidationException("Quantity must be >= 0", "QUANTITY_INVALID");

        var p = await _db.Product
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        var inv = await _db.Inventory.FirstOrDefaultAsync(x => x.productId == productId, ct);
        if (inv == null)
        {
            inv = new Inventory
            {
                productId = productId
            };
            _db.Inventory.Add(inv);
        }

        inv.quantity = quantity;
        inv.lastUpdated = DateTime.UtcNow;

        // Đồng bộ status theo inventory
        if (p.isAuction == true)
        {
            // Auction vẫn ưu tiên theo trạng thái thời gian đấu giá
            p.status = p.auctionEndTime.HasValue && p.auctionEndTime.Value <= DateTime.UtcNow
                ? ProductStatuses.Ended
                : ProductStatuses.Active;
        }
        else
        {
            // Buy now: quantity > 0 => ACTIVE, quantity = 0 => OUT_OF_STOCK
            p.status = quantity > 0
                ? ProductStatuses.Active
                : ProductStatuses.OutOfStock;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<ProductDetailDto> UploadImagesAsync(
        int sellerId,
        int productId,
        IFormFileCollection files,
        string baseUrl,
        CancellationToken ct)
    {
        var p = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        if (files == null || files.Count == 0)
            throw new ValidationException("No files uploaded", "FILES_EMPTY");

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        var folderRel = Path.Combine("uploads", "products", productId.ToString());
        var root = _env.WebRootPath ?? "wwwroot";
        var folderAbs = Path.Combine(root, folderRel);

        Directory.CreateDirectory(folderAbs);

        var urls = ProductImageJson.Read(p);

        if (urls.Count + files.Count > 10)
            throw new ValidationException("Maximum 10 images allowed", "IMAGES_LIMIT_EXCEEDED");

        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName);
            if (!allowed.Contains(ext))
                throw new ValidationException($"File type not allowed: {ext}", "FILE_TYPE_NOT_ALLOWED");

            if (f.Length > 5 * 1024 * 1024)
                throw new ValidationException("Each file must be <= 5MB", "FILE_TOO_LARGE");

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(folderAbs, fileName);

            await using var stream = File.Create(absPath);
            await f.CopyToAsync(stream, ct);

            var relUrl = "/" + Path.Combine(folderRel, fileName).Replace("\\", "/");
            var fullUrl = $"{baseUrl.TrimEnd('/')}{relUrl}";
            urls.Add(fullUrl);
        }

        ProductImageJson.Write(p, urls);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<ProductDetailDto>(p);
    }

    public async Task<ProductDetailDto> DeleteImageAsync(
        int sellerId,
        int productId,
        string imageUrl,
        CancellationToken ct)
    {
        var p = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        var urls = ProductImageJson.Read(p);
        var removed = urls.RemoveAll(x => string.Equals(x, imageUrl, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            throw new NotFoundException("Image not found", "PRODUCT_IMAGE_NOT_FOUND");

        ProductImageJson.Write(p, urls);
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<ProductDetailDto>(p);
    }

    public async Task<ProductDetailDto> UpdateStatusAsync(
        int sellerId,
        int productId,
        string status,
        CancellationToken ct)
    {
        status = (status ?? "").Trim().ToUpperInvariant();

        var allowed = new HashSet<string>
        {
            ProductStatuses.Draft,
            ProductStatuses.Active,
            ProductStatuses.Inactive,
            ProductStatuses.OutOfStock
        };

        if (!allowed.Contains(status))
            throw new ValidationException("Invalid status", "PRODUCT_STATUS_INVALID");

        var p = await _db.Product
            .Include(x => x.Inventory)
            .Include(x => x.Bid)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        if (p.isAuction == true && p.auctionEndTime.HasValue && p.auctionEndTime.Value <= DateTime.UtcNow)
            throw new ValidationException("Cannot change status of ended auction", "AUCTION_ALREADY_ENDED");

        var quantity = p.Inventory.Select(i => i.quantity ?? 0).FirstOrDefault();

        if (status == ProductStatuses.Active && p.isAuction != true && quantity <= 0)
            throw new ValidationException("Cannot activate product with zero quantity", "PRODUCT_OUT_OF_STOCK");

        p.status = status;
        await _db.SaveChangesAsync(ct);

        return _mapper.Map<ProductDetailDto>(p);
    }

    public async Task DeleteAsync(int sellerId, int productId, CancellationToken ct)
    {
        var p = await _db.Product
            .Include(x => x.Bid)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (p == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (p.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        if (p.Bid.Any())
            throw new ValidationException("Cannot delete product with existing bids", "PRODUCT_HAS_BIDS");

        p.isDeleted = true;
        p.deletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResponse<ProductListItemDto>> GetMyProductsAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.Product
            .AsNoTracking()
            .Include(p => p.Inventory)
            .Include(p => p.Bid)
            .Where(p => p.sellerId == sellerId && p.isDeleted != true);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(p => p.status == normalizedStatus);
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(p => p.id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        foreach (var p in rows)
        {
            if (p.isAuction == true && p.status == ProductStatuses.Active && p.auctionEndTime.HasValue && p.auctionEndTime.Value <= DateTime.UtcNow)
            {
                p.status = ProductStatuses.Ended;
            }
        }

        var items = _mapper.Map<List<ProductListItemDto>>(rows);
        return new PagedResponse<ProductListItemDto>(items, page, pageSize, total);
    }

    private static void ValidateCreate(CreateProductRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.title))
            throw new ValidationException("Title is required", "TITLE_REQUIRED");

        if (req.price <= 0)
            throw new ValidationException("Price must be > 0", "PRICE_INVALID");

        if (req.quantity < 0)
            throw new ValidationException("Quantity must be >= 0", "QUANTITY_INVALID");

        if (req.isAuction && req.auctionEndTime == null)
            throw new ValidationException("auctionEndTime is required for auction", "AUCTION_END_REQUIRED");

        if (req.isAuction && req.auctionEndTime <= DateTime.UtcNow)
            throw new ValidationException("auctionEndTime must be in the future", "AUCTION_END_INVALID");

        if (req.isAuction && req.quantity != 1)
            throw new ValidationException("Auction listing must have quantity = 1", "AUCTION_QUANTITY_INVALID");

        if (!req.isAuction && req.auctionEndTime != null)
            throw new ValidationException("auctionEndTime must be null for non-auction product", "AUCTION_END_NOT_ALLOWED");

        if (string.IsNullOrWhiteSpace(req.condition))
            throw new ValidationException("Condition is required", "PRODUCT_CONDITION_REQUIRED");

        if (!ProductConditions.All.Contains(req.condition.Trim().ToUpperInvariant()))
            throw new ValidationException("Invalid condition", "PRODUCT_CONDITION_INVALID");
    }

    private static void ValidateUpdate(UpdateProductRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.title))
            throw new ValidationException("Title is required", "TITLE_REQUIRED");

        if (req.price <= 0)
            throw new ValidationException("Price must be > 0", "PRICE_INVALID");

        if (req.isAuction && req.auctionEndTime == null)
            throw new ValidationException("auctionEndTime is required for auction", "AUCTION_END_REQUIRED");

        if (req.isAuction && req.auctionEndTime <= DateTime.UtcNow)
            throw new ValidationException("auctionEndTime must be in the future", "AUCTION_END_INVALID");

        if (!req.isAuction && req.auctionEndTime != null)
            throw new ValidationException("auctionEndTime must be null for non-auction product", "AUCTION_END_NOT_ALLOWED");

        if (string.IsNullOrWhiteSpace(req.condition))
            throw new ValidationException("Condition is required", "PRODUCT_CONDITION_REQUIRED");

        if (!ProductConditions.All.Contains(req.condition.Trim().ToUpperInvariant()))
            throw new ValidationException("Invalid condition", "PRODUCT_CONDITION_INVALID");
    }

    private static string NormalizeCondition(string condition)
        => condition.Trim().ToUpperInvariant();

    private static string DetermineStatus(bool isAuction, DateTime? auctionEndTime, int quantity, string? currentStatus = null)
    {
        if (isAuction)
        {
            if (auctionEndTime.HasValue && auctionEndTime.Value <= DateTime.UtcNow)
                return ProductStatuses.Ended;

            if (string.Equals(currentStatus, ProductStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                return ProductStatuses.Draft;

            if (string.Equals(currentStatus, ProductStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
                return ProductStatuses.Inactive;

            return ProductStatuses.Active;
        }

        if (string.Equals(currentStatus, ProductStatuses.Draft, StringComparison.OrdinalIgnoreCase))
            return ProductStatuses.Draft;

        if (string.Equals(currentStatus, ProductStatuses.Inactive, StringComparison.OrdinalIgnoreCase))
            return ProductStatuses.Inactive;

        return quantity <= 0 ? ProductStatuses.OutOfStock : ProductStatuses.Active;
    }

    private async Task IncreaseViewCountAsync(int productId, CancellationToken ct)
    {
        var p = await _db.Product.FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);
        if (p == null) return;

        p.viewCount = (p.viewCount ?? 0) + 1;
        await _db.SaveChangesAsync(ct);
    }
}