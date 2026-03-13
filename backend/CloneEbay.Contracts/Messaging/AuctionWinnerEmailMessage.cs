namespace CloneEbay.Contracts.Messaging;

public record AuctionWinnerEmailMessage(
    int ProductId,
    int WinnerUserId,
    decimal WinningBid,
    int OrderId
);