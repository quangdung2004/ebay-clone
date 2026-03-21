using CloneEbay.Contracts;
using CloneEbay.Contracts.Payments;
using CloneEbay.Contracts.Products;

namespace CloneEbay.Application.Payments;

public interface ISellerWalletService
{
    Task<SellerWalletDto> GetWalletAsync(int sellerId, CancellationToken ct);
    Task<PagedResponse<SellerSettlementHistoryItemDto>> GetSettlementHistoryAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken ct);
}