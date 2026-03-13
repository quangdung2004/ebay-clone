using CloneEbay.Contracts.Auctions;

namespace CloneEbay.Application.Auctions;

public interface IAuctionService
{
    Task<CloseAuctionResultDto> CloseAuctionAsync(int sellerId, int productId, CancellationToken ct);

    Task<AuctionWinnerDto> GetAuctionWinnerAsync(int productId, CancellationToken ct);
}