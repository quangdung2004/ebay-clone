namespace CloneEbay.Application.Notifications;

public interface IAuctionEventPublisher
{
    Task PublishWinnerEmailAsync(int productId, int winnerUserId, decimal winningBid, int orderId, CancellationToken ct);
}