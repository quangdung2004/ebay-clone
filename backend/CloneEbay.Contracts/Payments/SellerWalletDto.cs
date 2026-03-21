namespace CloneEbay.Contracts.Payments;

public sealed record SellerWalletDto(
    int sellerId,
    decimal pendingBalance,
    decimal availableBalance,
    decimal totalEarned,
    int trustLevel,
    bool isVerified,
    DateTime updatedAt
);