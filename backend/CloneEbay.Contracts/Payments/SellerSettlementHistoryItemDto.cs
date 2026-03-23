namespace CloneEbay.Contracts.Payments;

public sealed record SellerSettlementHistoryItemDto(
    int id,
    int orderId,
    int orderItemId,
    int productId,
    string productTitle,
    decimal grossAmount,
    decimal platformFee,
    decimal netAmount,
    string status,
    string? holdReason,
    DateTime? heldAt,
    DateTime? availableAt,
    DateTime? releasedAt
);