using CloneEbay.Domain.Entities;

namespace CloneEbay.Infrastructure.Orders;

public static class ShippingFeeCalculator
{
    public static decimal Calculate(
        Address buyerAddress,
        List<(Product product, int quantity, decimal unitPrice)> lines)
    {
        if (buyerAddress == null || lines == null || lines.Count == 0)
            return 0m;

        var buyerGeo = ShippingGeoCatalog.Resolve(buyerAddress.country);

        var groupedBySeller = lines
            .Where(x => x.product != null)
            .GroupBy(x => x.product.sellerId);

        decimal totalShippingFee = 0m;

        foreach (var sellerGroup in groupedBySeller)
        {
            var firstProduct = sellerGroup.First().product;
            var sellerGeo = ResolveSellerGeo(firstProduct);

            totalShippingFee += CalculateFeeByGeo(buyerGeo, sellerGeo);
        }

        return totalShippingFee;
    }

    private static decimal CalculateFeeByGeo(
        (string CountryCode, string Continent) buyerGeo,
        (string CountryCode, string Continent) sellerGeo)
    {
        if (buyerGeo.CountryCode == sellerGeo.CountryCode)
            return 3m;

        if (buyerGeo.Continent == sellerGeo.Continent)
            return 8m;

        return 15m;
    }

    private static (string CountryCode, string Continent) ResolveSellerGeo(Product product)
    {
        var sellerAddress = product.seller?.Address?
            .OrderByDescending(x => x.isDefault == true)
            .ThenBy(x => x.id)
            .FirstOrDefault();

        if (sellerAddress == null)
            return (ShippingGeoCatalog.DefaultCountryCode, ShippingGeoCatalog.DefaultContinent);

        return ShippingGeoCatalog.Resolve(sellerAddress.country);
    }
}