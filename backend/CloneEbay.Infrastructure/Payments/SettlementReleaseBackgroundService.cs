using CloneEbay.Application.Common;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Payments;

public sealed class SettlementReleaseBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementReleaseBackgroundService> _logger;

    public SettlementReleaseBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SettlementReleaseBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SettlementReleaseBackgroundService has started at {Time}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();

                await ReleaseEligibleSettlementsAsync(db, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while releasing eligible settlements.");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task ReleaseEligibleSettlementsAsync(CloneEbayDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var settlements = await db.SellerSettlement
            .Where(x => x.status == SettlementStatuses.OnHold
                         && x.availableAt != null
                         && x.availableAt <= now)
            .ToListAsync(ct);

        if (settlements.Count > 0)
        {
            _logger.LogInformation("Found {Count} settlements to release.", settlements.Count);

            foreach (var settlement in settlements)
            {
                var wallet = await db.SellerWallet
                    .FirstOrDefaultAsync(x => x.sellerId == settlement.sellerId, ct);

                if (wallet == null) continue;

                // Move from pending to available
                wallet.pendingBalance = Math.Max(0m, wallet.pendingBalance - settlement.netAmount);
                wallet.availableBalance += settlement.netAmount;
                wallet.totalEarned += settlement.netAmount;
                wallet.updatedAt = now;

                settlement.status = SettlementStatuses.Available;
                settlement.releasedAt = now;

                _logger.LogInformation("Released settlement {SettlementId} for seller {SellerId} with net amount {NetAmount}", 
                    settlement.id, settlement.sellerId, settlement.netAmount);
            }

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully saved changes for {Count} released settlements.", settlements.Count);
        }
    }
}