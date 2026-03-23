using CloneEbay.Application.Common;
using CloneEbay.Application.Notifications;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Persistence;

namespace CloneEbay.Infrastructure.Auctions;

/// <summary>
/// Shared logic for closing an auction product and creating the winner's order.
/// Used by both AuctionService (manual close) and AuctionClosingBackgroundService (auto close).
/// </summary>
public static class AuctionClosingHelper
{
    /// <summary>
    /// Determines the winning bid (highest amount, earliest time for tie-breaking).
    /// </summary>
    public static Bid? GetWinningBid(IEnumerable<Bid> bids)
    {
        return bids
            .OrderByDescending(x => x.amount)
            .ThenBy(x => x.bidTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Creates an order for the auction winner, updates inventory and product status.
    /// Must be called within a transaction. Does NOT call SaveChangesAsync — caller must do that.
    /// </summary>
    public static OrderTable CreateWinnerOrder(
        CloneEbayDbContext db,
        Product product,
        Bid winningBid)
    {
        var order = new OrderTable
        {
            buyerId = winningBid.bidderId,
            addressId = null,
            orderDate = DateTime.UtcNow,
            totalPrice = winningBid.amount,
            status = OrderStatuses.PendingPayment
        };

        db.OrderTable.Add(order);
        return order;
    }

    /// <summary>
    /// Creates the order item, updates product winner/status/inventory.
    /// Must be called after SaveChanges (so order.id is populated).
    /// </summary>
    public static void FinalizeAuctionWin(
        CloneEbayDbContext db,
        Product product,
        Bid winningBid,
        int orderId)
    {
        db.OrderItem.Add(new OrderItem
        {
            orderId = orderId,
            productId = product.id,
            quantity = 1,
            unitPrice = winningBid.amount
        });

        product.winnerUserId = winningBid.bidderId;
        product.auctionOrderId = orderId;
        product.status = ProductStatuses.Sold;

        var inventory = product.Inventory.FirstOrDefault();
        if (inventory != null)
        {
            inventory.quantity = 0;
            inventory.lastUpdated = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Marks an auction as ended (no bids).
    /// </summary>
    public static void MarkEndedNoBids(Product product)
    {
        product.status = ProductStatuses.Ended;
    }

    /// <summary>
    /// Checks if a product has already been closed (already has winner/order/status).
    /// </summary>
    public static bool IsAlreadyClosed(Product product)
    {
        return product.auctionOrderId.HasValue
               || product.winnerUserId.HasValue
               || product.status == ProductStatuses.Sold
               || product.status == ProductStatuses.Ended;
    }
}
