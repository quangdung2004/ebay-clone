using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CloneEbay.Infrastructure.Payments;

public sealed class SettlementReleaseBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SettlementReleaseBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();

                var now = DateTime.UtcNow;

                var settlements = await db.SellerSettlement
                    .Where(x => x.status == "ON_HOLD"
                             && x.availableAt != null
                             && x.availableAt <= now)
                    .ToListAsync(stoppingToken);

                foreach (var settlement in settlements)
                {
                    var wallet = await db.SellerWallet
                        .FirstOrDefaultAsync(x => x.sellerId == settlement.sellerId, stoppingToken);

                    if (wallet == null)
                        continue;

                    wallet.pendingBalance -= settlement.netAmount;
                    wallet.availableBalance += settlement.netAmount;
                    wallet.totalEarned += settlement.netAmount;
                    wallet.updatedAt = now;

                    settlement.status = "AVAILABLE";
                    settlement.releasedAt = now;
                }

                if (settlements.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch
            {
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}