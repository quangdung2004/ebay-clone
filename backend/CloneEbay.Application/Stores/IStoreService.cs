using CloneEbay.Contracts.Stores;

namespace CloneEbay.Application.Stores;

public interface IStoreService
{
    Task<StoreDto> CreateAsync(int sellerId, CreateStoreRequest req, CancellationToken ct);
    Task<StoreDto> GetMyStoreAsync(int sellerId, CancellationToken ct);
    Task<StoreDto> UpdateMyStoreAsync(int sellerId, UpdateStoreRequest req, CancellationToken ct);
}