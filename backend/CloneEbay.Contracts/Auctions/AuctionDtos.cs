namespace CloneEbay.Contracts.Auctions;

public record AuctionWinnerDto(
    int productId,
    bool isClosed,
    bool hasWinner,
    int? winnerUserId,
    decimal? winningBid,
    int bidCount,
    int? orderId,
    string productStatus
);

public record CloseAuctionResultDto(
    int productId,
    bool hasWinner,
    int? winnerUserId,
    decimal? winningBid,
    int bidCount,
    int? orderId,
    string productStatus,
    string message
);