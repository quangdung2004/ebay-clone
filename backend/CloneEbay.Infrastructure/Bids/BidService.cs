using System.Data;
using CloneEbay.Application.Bids;
using CloneEbay.Contracts.Bids;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Auctions;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Bids;

public sealed class BidService : IBidService
{
    private readonly CloneEbayDbContext _db;
    private readonly AuctionRealtimeNotifier _notifier;

    public BidService(CloneEbayDbContext db, AuctionRealtimeNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task<PlaceBidResultDto> PlaceBidAsync(int bidderId, int productId, PlaceBidRequest req, CancellationToken ct)
    {
        if (req.amount <= 0)
            throw new ValidationException("Bid amount must be > 0", "BID_AMOUNT_INVALID");

        PlaceBidResultDto result;

        await using (var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct))
        {
            var product = await _db.Product
                .Include(x => x.Bid)
                .Include(x => x.Inventory)
                .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

            if (product == null)
                throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

            if (product.isAuction != true)
                throw new ValidationException("This product is not an auction listing", "PRODUCT_NOT_AUCTION");

            if (product.sellerId == bidderId)
                throw new ValidationException("You cannot bid on your own product", "BID_SELF_NOT_ALLOWED");

            if (product.auctionEndTime == null)
                throw new ValidationException("Auction end time is missing", "AUCTION_END_REQUIRED");

            if (product.auctionEndTime <= DateTime.UtcNow)
            {
                product.status = ProductStatuses.Ended;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                throw new ValidationException("Auction already ended", "AUCTION_ALREADY_ENDED");
            }

            if (!string.Equals(product.status, ProductStatuses.Active, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException("Auction is not active", "AUCTION_NOT_ACTIVE");

            var currentBid = product.Bid.Any()
                ? product.Bid.Max(x => x.amount) ?? 0
                : product.price ?? 0;

            if (req.amount <= currentBid)
                throw new ValidationException("Bid amount must be greater than current bid", "BID_TOO_LOW");

            var bid = new Bid
            {
                productId = productId,
                bidderId = bidderId,
                amount = req.amount,
                bidTime = DateTime.UtcNow
            };

            _db.Bid.Add(bid);
            await _db.SaveChangesAsync(ct);

            var bidCount = await _db.Bid.CountAsync(x => x.productId == productId, ct);
            var latestCurrentBid = await _db.Bid
                .Where(x => x.productId == productId)
                .MaxAsync(x => x.amount, ct) ?? 0;

            result = new PlaceBidResultDto(
                productId: productId,
                yourBid: req.amount,
                currentBid: latestCurrentBid,
                bidCount: bidCount,
                isLeading: req.amount == latestCurrentBid,
                auctionEndTime: product.auctionEndTime
            );

            await tx.CommitAsync(ct);
        }

        try
        {
            await _notifier.NotifyBidPlacedAsync(
                productId: result.productId,
                currentBid: result.currentBid,
                bidCount: result.bidCount,
                bidderId: bidderId,
                ct: ct
            );
        }
        catch
        {
            // Không throw để tránh làm fail request sau khi bid đã commit thành công.
            // Có thể thêm logger ở đây nếu muốn.
        }

        return result;
    }

    public async Task<PagedResponse<BidHistoryItemDto>> GetBidHistoryAsync(int productId, int page, int pageSize, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var productExists = await _db.Product
            .AsNoTracking()
            .AnyAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (!productExists)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        var query = _db.Bid
            .AsNoTracking()
            .Where(x => x.productId == productId)
            .OrderByDescending(x => x.amount)
            .ThenBy(x => x.bidTime);

        var total = await query.CountAsync(ct);

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(x => new BidHistoryItemDto(
            x.id,
            x.productId,
            x.bidderId,
            x.amount ?? 0,
            x.bidTime
        )).ToList();

        return new PagedResponse<BidHistoryItemDto>(items, page, pageSize, total);
    }
}