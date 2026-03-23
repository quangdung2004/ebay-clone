using CloneEbay.Application.Common;
using CloneEbay.Application.Notifications;
using CloneEbay.Infrastructure.Persistence;
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
            await ProcessSingleAuctionAsync(db, publisher, hub, productId, ct);
        }
    }

    private static async Task ProcessSingleAuctionAsync(
        CloneEbayDbContext db,
        IAuctionEventPublisher publisher,
        AuctionRealtimeNotifier hub,
        int productId,
        CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var product = await db.Product
            .Include(x => x.Bid)
            .Include(x => x.Inventory)
            .FirstOrDefaultAsync(x => x.id == productId && x.isDeleted != true, ct);

        if (product == null || AuctionClosingHelper.IsAlreadyClosed(product))
        {
            await tx.CommitAsync(ct);
            return;
        }

        var winningBid = AuctionClosingHelper.GetWinningBid(product.Bid);

        if (winningBid == null)
        {
            AuctionClosingHelper.MarkEndedNoBids(product);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await hub.NotifyAuctionClosedAsync(product.id, false, null, null, ct);
            return;
        }

        var order = AuctionClosingHelper.CreateWinnerOrder(db, product, winningBid);
        await db.SaveChangesAsync(ct);

        AuctionClosingHelper.FinalizeAuctionWin(db, product, winningBid, order.id);
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