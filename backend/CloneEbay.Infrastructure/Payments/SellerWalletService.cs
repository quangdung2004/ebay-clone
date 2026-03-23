using CloneEbay.Application.Payments;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Payments;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Common.Helpers;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Payments;

public sealed class SellerWalletService : ISellerWalletService
{
    private readonly CloneEbayDbContext _db;

    public SellerWalletService(CloneEbayDbContext db)
    {
        _db = db;
    }

    public async Task<SellerWalletDto> GetWalletAsync(int sellerId, CancellationToken ct)
    {
        var wallet = await _db.SellerWallet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);

        var trust = await _db.SellerTrustProfile
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);

        return new SellerWalletDto(
            sellerId: sellerId,
            pendingBalance: wallet?.pendingBalance ?? 0m,
            availableBalance: wallet?.availableBalance ?? 0m,
            totalEarned: wallet?.totalEarned ?? 0m,
            trustLevel: trust?.level ?? 1,
            isVerified: trust?.isVerified ?? false,
            updatedAt: wallet?.updatedAt ?? DateTime.UtcNow
        );
    }

    public async Task<PagedResponse<SellerSettlementHistoryItemDto>> GetSettlementHistoryAsync(
        int sellerId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _db.SellerSettlement
            .AsNoTracking()
            .Include(x => x.orderItem)
                .ThenInclude(x => x!.product)
            .Where(x => x.sellerId == sellerId)
            .OrderByDescending(x => x.heldAt)
            .ThenByDescending(x => x.id);

        var total = await query.CountAsync(ct);

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = entities.Select(x => new SellerSettlementHistoryItemDto(
                x.id,
                x.orderId,
                x.orderItemId,
                x.orderItem != null ? (x.orderItem.productId ?? 0) : 0,
                x.orderItem != null && x.orderItem.product != null
                    ? x.orderItem.product.title ?? "Unknown product"
                    : "Unknown product",
                x.grossAmount,
                x.platformFee,
                x.netAmount,
                x.status,
                x.holdReason,
                x.heldAt,
                x.availableAt,
                x.releasedAt
            )).ToList();

        return new PagedResponse<SellerSettlementHistoryItemDto>(items, page, pageSize, total);
    }
}