using CloneEbay.Application.Shipping;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class ShippingService : IShippingService
{
    private readonly CloneEbayDbContext _db;

    public ShippingService(CloneEbayDbContext db)
    {
        _db = db;
    }

    public async Task<ShipmentQuoteResult> QuoteAsync(
        Address destination,
        IReadOnlyList<(Product product, int quantity)> items,
        CancellationToken ct)
    {
        if (items == null || items.Count == 0)
            return new ShipmentQuoteResult(0m, Array.Empty<ShipmentDraft>());

        var groups = items
            .GroupBy(x => x.product.sellerId!.Value)
            .ToList();

        var drafts = new List<ShipmentDraft>();

        foreach (var group in groups)
        {
            var sellerId = group.Key;

            var origin = await _db.Address
                .AsNoTracking()
                .Where(x =>
                    x.userId == sellerId &&
                    (
                        x.isShippingOrigin == true ||
                        x.addressType == "SELLER_ORIGIN"
                    ))
                .OrderByDescending(x => x.isShippingOrigin == true)
                .ThenByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);

            origin ??= await _db.Address
                .AsNoTracking()
                .Where(x => x.userId == sellerId)
                .OrderByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);

            if (origin == null)
                throw new ValidationException(
                    $"Seller {sellerId} does not have origin address",
                    "SELLER_ORIGIN_ADDRESS_MISSING");

            var totalQuantity = group.Sum(x => x.quantity);

            var totalWeightGrams = group.Sum(x =>
                (x.product.weightGrams ?? 500) * x.quantity);

            decimal shippingCost = 30000m;

            if (totalQuantity > 1)
                shippingCost += (totalQuantity - 1) * 5000m;

            if (totalWeightGrams > 1000)
            {
                var extraKg = (int)Math.Ceiling((totalWeightGrams - 1000) / 1000m);
                shippingCost += extraKg * 10000m;
            }

            var maxHandlingDays = group.Max(x => x.product.handlingDays ?? 1);

            drafts.Add(new ShipmentDraft(
                sellerId: sellerId,
                originAddressId: origin.id,
                shippingMethod: "STANDARD",
                carrier: "GHN",
                shippingCost: shippingCost,
                estimatedDeliveryDate: DateTime.UtcNow.AddDays(maxHandlingDays + 3)
            ));
        }

        return new ShipmentQuoteResult(
            shippingTotal: drafts.Sum(x => x.shippingCost),
            shipments: drafts
        );
    }
}