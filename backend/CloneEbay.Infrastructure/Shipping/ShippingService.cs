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
                .Where(x => x.userId == sellerId)
                .OrderByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);

            if (origin == null)
                throw new ValidationException(
                    $"Seller {sellerId} does not have address",
                    "SELLER_ADDRESS_MISSING");

            var totalQuantity = group.Sum(x => x.quantity);

            var totalWeightGrams = group.Sum(x =>
                (x.product.weightGrams ?? 500) * x.quantity);

            decimal distanceKm = 0m;

            if (origin.latitude.HasValue && origin.longitude.HasValue &&
                destination.latitude.HasValue && destination.longitude.HasValue)
            {
                distanceKm = CalculateDistanceKm(
                    (double)origin.latitude.Value,
                    (double)origin.longitude.Value,
                    (double)destination.latitude.Value,
                    (double)destination.longitude.Value
                );
            }

            decimal shippingCost = 1m;

            if (distanceKm > 5)
            {
                shippingCost += Math.Ceiling(distanceKm - 5) * 0.1m;
            }

            if (totalQuantity > 1)
                shippingCost += (totalQuantity - 1) * 0.16m;

            if (totalWeightGrams > 1000)
            {
                var extraKg = (int)Math.Ceiling((totalWeightGrams - 1000) / 1000m);
                shippingCost += extraKg * 0.04m;
            }

            var maxHandlingDays = group.Max(x => x.product.handlingDays ?? 1);

            drafts.Add(new ShipmentDraft(
                sellerId: sellerId,
                originAddressId: origin.id,
                shippingMethod: "STANDARD",
                carrier: "GHN",
                shippingCost: decimal.Round(shippingCost, 2, MidpointRounding.AwayFromZero),
                estimatedDeliveryDate: DateTime.UtcNow.AddDays(maxHandlingDays + 3)
            ));
        }

        return new ShipmentQuoteResult(
            shippingTotal: drafts.Sum(x => x.shippingCost),
            shipments: drafts
        );

    }
    private static decimal CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371d;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}