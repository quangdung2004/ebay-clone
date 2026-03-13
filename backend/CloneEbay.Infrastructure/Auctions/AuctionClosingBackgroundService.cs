using CloneEbay.Application.Notifications;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Auctions;

public sealed class AuctionClosingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionClosingBackgroundService> _logger;

    public AuctionClosingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionClosingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredAuctionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing expired auctions");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessExpiredAuctionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IAuctionEventPublisher>();
        var hub = scope.ServiceProvider.GetRequiredService<AuctionRealtimeNotifier>();

        var expiredIds = await db.Product
            .AsNoTracking()
            .Where(x =>
                x.isDeleted != true &&
                x.isAuction == true &&
                x.status == ProductStatuses.Active &&
                x.auctionEndTime != null &&
                x.auctionEndTime <= DateTime.UtcNow)
            .Select(x => x.id)
            .Take(50)
            .ToListAsync(ct);

        foreach (var productId in expiredIds)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var product = await db.Product
                .Include(x => x.Bid)
                .Include(x => x.Inventory)
                .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

            if (product == null)
            {
                await tx.CommitAsync(ct);
                continue;
            }

            if (product.status == ProductStatuses.Sold || product.status == ProductStatuses.Ended)
            {
                await tx.CommitAsync(ct);
                continue;
            }

            if (product.auctionOrderId.HasValue || product.winnerUserId.HasValue)
            {
                await tx.CommitAsync(ct);
                continue;
            }

            var winningBid = product.Bid
                .OrderByDescending(x => x.amount)
                .ThenBy(x => x.bidTime)
                .FirstOrDefault();

            if (winningBid == null)
            {
                product.status = ProductStatuses.Ended;
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await hub.NotifyAuctionClosedAsync(product.id, false, null, null, ct);
                continue;
            }

            var order = new Domain.Entities.OrderTable
            {
                buyerId = winningBid.bidderId,
                addressId = null,
                orderDate = DateTime.UtcNow,
                totalPrice = winningBid.amount,
                status = "PENDING_PAYMENT"
            };

            db.OrderTable.Add(order);
            await db.SaveChangesAsync(ct);

            db.OrderItem.Add(new Domain.Entities.OrderItem
            {
                orderId = order.id,
                productId = product.id,
                quantity = 1,
                unitPrice = winningBid.amount
            });

            product.winnerUserId = winningBid.bidderId;
            product.auctionOrderId = order.id;
            product.status = ProductStatuses.Sold;

            var inv = product.Inventory.FirstOrDefault();
            if (inv != null)
            {
                inv.quantity = 0;
                inv.lastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await publisher.PublishWinnerEmailAsync(
                product.id,
                winningBid.bidderId!.Value,
                winningBid.amount ?? 0,
                order.id,
                ct);

            await hub.NotifyAuctionClosedAsync(product.id, true, winningBid.bidderId, winningBid.amount, ct);
        }
    }
}