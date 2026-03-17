using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Shipping;

public interface IShippingService
{
    Task<ShipmentQuoteResult> QuoteAsync(
        Address destination,
        IReadOnlyList<(Product product, int quantity)> items,
        CancellationToken ct);
}

public record ShipmentQuoteResult(
    decimal shippingTotal,
    IReadOnlyList<ShipmentDraft> shipments
);

public record ShipmentDraft(
    int sellerId,
    int originAddressId,
    string shippingMethod,
    string carrier,
    decimal shippingCost,
    DateTime? estimatedDeliveryDate
);