using CloneEbay.Application.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CloneEbay.Infrastructure.Auctions;

public sealed class AuctionRealtimeNotifier
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public AuctionRealtimeNotifier(IHubContext<AuctionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyBidPlacedAsync(
        int productId,
        decimal currentBid,
        int bidCount,
        int bidderId,
        CancellationToken ct = default)
    {
        return _hubContext.Clients
            .Group($"auction-product-{productId}")
            .SendAsync("BidPlaced", new
            {
                productId,
                currentBid,
                bidCount,
                bidderId
            }, ct);
    }

    public Task NotifyAuctionClosedAsync(
        int productId,
        bool hasWinner,
        int? winnerUserId,
        decimal? winningBid,
        CancellationToken ct = default)
    {
        return _hubContext.Clients
            .Group($"auction-product-{productId}")
            .SendAsync("AuctionClosed", new
            {
                productId,
                hasWinner,
                winnerUserId,
                winningBid
            }, ct);
    }
}