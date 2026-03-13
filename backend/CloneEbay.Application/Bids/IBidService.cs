using CloneEbay.Contracts.Bids;
using CloneEbay.Contracts.Products;

namespace CloneEbay.Application.Bids;

public interface IBidService
{
    Task<PlaceBidResultDto> PlaceBidAsync(int bidderId, int productId, PlaceBidRequest req, CancellationToken ct);

    Task<PagedResponse<BidHistoryItemDto>> GetBidHistoryAsync(int productId, int page, int pageSize, CancellationToken ct);
}