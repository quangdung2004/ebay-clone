using CloneEbay.Application.Auctions;
using CloneEbay.Application.Common;
using CloneEbay.Contracts.Auctions;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CloneEbay.Application.Notifications;

namespace CloneEbay.Infrastructure.Auctions;

public sealed class AuctionService : IAuctionService
{
    private readonly CloneEbayDbContext _db;
    private readonly IAuctionEventPublisher _publisher;
    private readonly AuctionRealtimeNotifier _notifier;

    public AuctionService(CloneEbayDbContext db, IAuctionEventPublisher publisher, AuctionRealtimeNotifier notifier)
    {
        _db = db;
        _publisher = publisher;
        _notifier = notifier;
    }

    public async Task<CloseAuctionResultDto> CloseAuctionAsync(int sellerId, int productId, CancellationToken ct)
    {
        var product = await _db.Product
            .Include(x => x.Bid)
            .Include(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (product == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (product.sellerId != sellerId)
            throw new ForbiddenException("You are not the owner", "PRODUCT_FORBIDDEN");

        if (product.isAuction != true)
            throw new ValidationException("This product is not an auction listing", "PRODUCT_NOT_AUCTION");

        if (product.auctionEndTime == null)
            throw new ValidationException("Auction end time is missing", "AUCTION_END_REQUIRED");

        // If already closed, return existing result
        if (AuctionClosingHelper.IsAlreadyClosed(product))
            return BuildAlreadyClosedResult(product);

        var winningBid = AuctionClosingHelper.GetWinningBid(product.Bid);

        // No bids
        if (winningBid == null)
            return await CloseWithNoBidsAsync(product, ct);

        // Has winner → create order
        return await CloseWithWinnerAsync(product, winningBid, ct);
    }

    public async Task<AuctionWinnerDto> GetAuctionWinnerAsync(int productId, CancellationToken ct)
    {
        var product = await _db.Product
            .Include(x => x.Bid)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (product == null)
            throw new NotFoundException("Product not found", "PRODUCT_NOT_FOUND");

        if (product.isAuction != true)
            throw new ValidationException("This product is not an auction listing", "PRODUCT_NOT_AUCTION");

        var winningBid = AuctionClosingHelper.GetWinningBid(product.Bid);
        var isClosed = product.auctionEndTime.HasValue && product.auctionEndTime.Value <= DateTime.UtcNow;

        return new AuctionWinnerDto(
            productId: product.id,
            isClosed: isClosed,
            hasWinner: product.winnerUserId.HasValue,
            winnerUserId: product.winnerUserId,
            winningBid: winningBid?.amount,
            bidCount: product.Bid.Count,
            orderId: product.auctionOrderId,
            productStatus: product.status ?? ProductStatuses.Active
        );
    }

    // ── Private helpers ─────────────────────────────────────────────

    private static CloseAuctionResultDto BuildAlreadyClosedResult(Product product)
    {
        var winningBid = AuctionClosingHelper.GetWinningBid(product.Bid);

        return new CloseAuctionResultDto(
            productId: product.id,
            hasWinner: product.winnerUserId.HasValue,
            winnerUserId: product.winnerUserId,
            winningBid: winningBid?.amount,
            bidCount: product.Bid.Count,
            orderId: product.auctionOrderId,
            productStatus: product.status ?? ProductStatuses.Ended,
            message: "Auction already closed"
        );
    }

    private async Task<CloseAuctionResultDto> CloseWithNoBidsAsync(Product product, CancellationToken ct)
    {
        AuctionClosingHelper.MarkEndedNoBids(product);
        await _db.SaveChangesAsync(ct);

        await _notifier.NotifyAuctionClosedAsync(product.id, false, null, null, ct);

        return new CloseAuctionResultDto(
            productId: product.id,
            hasWinner: false,
            winnerUserId: null,
            winningBid: null,
            bidCount: 0,
            orderId: null,
            productStatus: product.status,
            message: "Auction closed with no bids"
        );
    }

    private async Task<CloseAuctionResultDto> CloseWithWinnerAsync(Product product, Bid winningBid, CancellationToken ct)
    {
        var order = AuctionClosingHelper.CreateWinnerOrder(_db, product, winningBid);
        await _db.SaveChangesAsync(ct);

        AuctionClosingHelper.FinalizeAuctionWin(_db, product, winningBid, order.id);
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishWinnerEmailAsync(
            product.id,
            winningBid.bidderId!.Value,
            winningBid.amount ?? 0,
            order.id,
            ct);

        await _notifier.NotifyAuctionClosedAsync(
            product.id,
            true,
            winningBid.bidderId,
            winningBid.amount,
            ct);

        return new CloseAuctionResultDto(
            productId: product.id,
            hasWinner: true,
            winnerUserId: winningBid.bidderId,
            winningBid: winningBid.amount,
            bidCount: product.Bid.Count,
            orderId: order.id,
            productStatus: product.status,
            message: "Auction closed successfully"
        );
    }
}