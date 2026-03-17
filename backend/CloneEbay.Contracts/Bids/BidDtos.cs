using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Bids;

public record PlaceBidRequest(
    [param: Range(typeof(decimal), "0.01", "100000.00", ErrorMessage = ValidationMessages.PositiveNumber)]
    decimal amount
);

public record BidHistoryItemDto(
    int id,
    int? productId,
    int? bidderId,
    decimal amount,
    DateTime? bidTime
) {
    public string currency { get; init; } = "USD";
}

public record PlaceBidResultDto(
    int productId,
    decimal yourBid,
    decimal currentBid,
    int bidCount,
    bool isLeading,
    DateTime? auctionEndTime
) {
    public string currency { get; init; } = "USD";
}