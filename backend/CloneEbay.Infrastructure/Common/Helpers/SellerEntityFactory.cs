using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Common.Helpers;

/// <summary>
/// Shared helper to get or create SellerWallet and SellerTrustProfile.
/// Used by PaymentService (on capture) and ShippingService (on delivery).
/// </summary>
public static class SellerEntityFactory
{
    public static async Task<SellerWallet> GetOrCreateWalletAsync(
        CloneEbayDbContext db,
        int sellerId,
        CancellationToken ct)
    {
        var wallet = await db.SellerWallet.FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);
        if (wallet != null)
            return wallet;

        wallet = new SellerWallet
        {
            sellerId = sellerId,
            pendingBalance = 0m,
            availableBalance = 0m,
            totalEarned = 0m,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        db.SellerWallet.Add(wallet);
        return wallet;
    }

    public static async Task<SellerTrustProfile> GetOrCreateTrustProfileAsync(
        CloneEbayDbContext db,
        int sellerId,
        CancellationToken ct)
    {
        var trustProfile = await db.SellerTrustProfile.FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);
        if (trustProfile != null)
            return trustProfile;

        trustProfile = new SellerTrustProfile
        {
            sellerId = sellerId,
            level = 1,
            completedOrders = 0,
            successfulDeliveries = 0,
            refundCount = 0,
            disputeCount = 0,
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        db.SellerTrustProfile.Add(trustProfile);
        return trustProfile;
    }

    /// <summary>
    /// Default wallet DTO for sellers that don't have a wallet yet.
    /// </summary>
    public static SellerWallet CreateDefaultWallet(int sellerId) => new()
    {
        sellerId = sellerId,
        pendingBalance = 0m,
        availableBalance = 0m,
        totalEarned = 0m,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Default trust profile for sellers without one.
    /// </summary>
    public static SellerTrustProfile CreateDefaultTrustProfile(int sellerId) => new()
    {
        sellerId = sellerId,
        level = 1,
        completedOrders = 0,
        successfulDeliveries = 0,
        refundCount = 0,
        disputeCount = 0,
        isVerified = false,
        createdAt = DateTime.UtcNow,
        updatedAt = DateTime.UtcNow
    };
}
