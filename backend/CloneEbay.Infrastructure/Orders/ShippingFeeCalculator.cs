using CloneEbay.Domain.Entities;

namespace CloneEbay.Infrastructure.Orders;

public static class ShippingFeeCalculator
{
    public static decimal Calculate(
        Address shippingAddress,
        List<(Product product, int quantity, decimal unitPrice)> lines)
    {
        var subtotal = lines.Sum(x => x.unitPrice * x.quantity);
        var totalQty = lines.Sum(x => x.quantity);

        if (subtotal >= 200m)
            return 0m;

        var baseFee = shippingAddress.country?.Trim().ToUpperInvariant() == "VIETNAM"
            ? 2m
            : 8m;

        var extraItemFee = Math.Max(0, totalQty - 1) * 1m;

        return baseFee + extraItemFee;
    }
}