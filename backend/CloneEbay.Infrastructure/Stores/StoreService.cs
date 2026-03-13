using CloneEbay.Application.Stores;
using CloneEbay.Contracts.Stores;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Stores;

public sealed class StoreService : IStoreService
{
    private readonly CloneEbayDbContext _db;

    public StoreService(CloneEbayDbContext db)
    {
        _db = db;
    }

    public async Task<StoreDto> CreateAsync(int sellerId, CreateStoreRequest req, CancellationToken ct)
    {
        Validate(req.storeName);

        var existing = await _db.Store
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);

        if (existing != null)
            throw new ConflictException("You already have a store", "STORE_ALREADY_EXISTS");

        var store = new Store
        {
            sellerId = sellerId,
            storeName = req.storeName.Trim(),
            description = req.description?.Trim(),
            bannerImageURL = req.bannerImageURL?.Trim()
        };

        _db.Store.Add(store);
        await _db.SaveChangesAsync(ct);

        return ToDto(store);
    }

    public async Task<StoreDto> GetMyStoreAsync(int sellerId, CancellationToken ct)
    {
        var store = await _db.Store
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);

        if (store == null)
            throw new NotFoundException("Store not found", "STORE_NOT_FOUND");

        return ToDto(store);
    }

    public async Task<StoreDto> UpdateMyStoreAsync(int sellerId, UpdateStoreRequest req, CancellationToken ct)
    {
        Validate(req.storeName);

        var store = await _db.Store
            .FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);

        if (store == null)
            throw new NotFoundException("Store not found", "STORE_NOT_FOUND");

        store.storeName = req.storeName.Trim();
        store.description = req.description?.Trim();
        store.bannerImageURL = req.bannerImageURL?.Trim();

        await _db.SaveChangesAsync(ct);

        return ToDto(store);
    }

    private static void Validate(string storeName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ValidationException("Store name is required", "STORE_NAME_REQUIRED");
    }

    private static StoreDto ToDto(Store s) => new(
        s.id,
        s.sellerId ?? 0,
        s.storeName ?? "",
        s.description,
        s.bannerImageURL
    );
}