using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Orders;

public class OrderAutoCancelBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderAutoCancelBackgroundService> _logger;
    private const int PaymentTimeoutMinutes = 5;

    public OrderAutoCancelBackgroundService(IServiceProvider serviceProvider, ILogger<OrderAutoCancelBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Auto-Cancel Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CancelTimedOutOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cancelling timed-out orders.");
            }

            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Order Auto-Cancel Background Service is stopping.");
    }

    private async Task CancelTimedOutOrdersAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloneEbayDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IOrderEmailService>();

        var cutoffTime = DateTime.UtcNow.AddMinutes(-PaymentTimeoutMinutes);

        var timedOutOrders = await db.OrderTable
            .Include(x => x.buyer)
            .Include(x => x.OrderItem)
                .ThenInclude(i => i.product)
                    .ThenInclude(p => p!.Inventory)
            .Where(x => x.status == OrderStatuses.PendingPayment && x.orderDate < cutoffTime)
            .ToListAsync(ct);

        if (!timedOutOrders.Any()) return;

        _logger.LogInformation("Found {Count} timed-out orders pending payment. Cancelling them...", timedOutOrders.Count);

        foreach (var order in timedOutOrders)
        {
            var oldStatus = order.status;
            order.status = OrderStatuses.Cancelled;

            foreach (var item in order.OrderItem)
            {
                if (item.product == null) continue;

                var inventory = item.product.Inventory.FirstOrDefault();
                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        productId = item.product.id,
                        quantity = 0,
                        lastUpdated = DateTime.UtcNow
                    };
                    db.Inventory.Add(inventory);
                }

                inventory.quantity = (inventory.quantity ?? 0) + (item.quantity ?? 0);
                inventory.lastUpdated = DateTime.UtcNow;
                item.product.status = ProductStatuses.Active;
            }
            
            _logger.LogInformation("Order #{OrderId} cancelled due to payment timeout.", order.id);

            // Notify buyer about the cancellation (optional but good)
            try
            {
                await emailService.SendOrderStatusChangedEmailAsync(order, oldStatus!, order.status, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation email for order #{OrderId}", order.id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
