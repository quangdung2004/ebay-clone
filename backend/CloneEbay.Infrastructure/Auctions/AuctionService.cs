using CloneEbay.Application.Auctions;
using CloneEbay.Contracts.Auctions;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
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

        // Removed ValidationException for checking if auction has not ended yet, allowing early closing by sellers

        // Nếu đã close trước đó rồi thì trả kết quả cũ luôn, không tạo lại order
        if (product.auctionOrderId.HasValue || product.winnerUserId.HasValue || product.status == ProductStatuses.Sold || product.status == ProductStatuses.Ended)
        {
            var winningBidExisting = product.Bid
                .OrderByDescending(x => x.amount)
                .ThenBy(x => x.bidTime)
                .FirstOrDefault();

            var hasWinnerExisting = product.winnerUserId.HasValue;

            return new CloseAuctionResultDto(
                productId: product.id,
                hasWinner: hasWinnerExisting,
                winnerUserId: product.winnerUserId,
                winningBid: winningBidExisting?.amount,
                bidCount: product.Bid.Count,
                orderId: product.auctionOrderId,
                productStatus: product.status ?? ProductStatuses.Ended,
                message: "Auction already closed"
            );
        }

        var winningBid = product.Bid
            .OrderByDescending(x => x.amount)
            .ThenBy(x => x.bidTime)
            .FirstOrDefault();

        // Không có ai bid
        if (winningBid == null)
        {
            product.status = ProductStatuses.Ended;
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

        // Có winner -> tạo order
        var order = new OrderTable
        {
            buyerId = winningBid.bidderId,
            addressId = null,
            orderDate = DateTime.UtcNow,
            totalPrice = winningBid.amount,
            status = "PENDING_PAYMENT"
        };

        _db.OrderTable.Add(order);
        await _db.SaveChangesAsync(ct);

        var orderItem = new OrderItem
        {
            orderId = order.id,
            productId = product.id,
            quantity = 1,
            unitPrice = winningBid.amount
        };

        _db.OrderItem.Add(orderItem);

        product.winnerUserId = winningBid.bidderId;
        product.auctionOrderId = order.id;
        product.status = ProductStatuses.Sold;

        var inventory = product.Inventory.FirstOrDefault();
        if (inventory != null)
        {
            inventory.quantity = 0;
            inventory.lastUpdated = DateTime.UtcNow;
        }

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

        var winningBid = product.Bid
            .OrderByDescending(x => x.amount)
            .ThenBy(x => x.bidTime)
            .FirstOrDefault();

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
}