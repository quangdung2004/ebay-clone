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

            var sellerAddress = await _db.Address
                .AsNoTracking()
                .Where(x => x.userId == sellerId)
                .OrderByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);

            if (sellerAddress == null)
                throw new ValidationException(
                    $"Seller {sellerId} does not have address",
                    "SELLER_ADDRESS_MISSING");

            var originWarehouse = await FindNearestWarehouseAsync(sellerAddress, ct);

            if (originWarehouse == null)
                throw new ValidationException(
                    "No warehouse available",
                    "WAREHOUSE_NOT_FOUND");

            var totalQuantity = group.Sum(x => x.quantity);

            var totalWeightGrams = group.Sum(x =>
                (x.product.weightGrams ?? 500) * x.quantity);

            decimal distanceKm = 0m;

            if (originWarehouse.latitude.HasValue && originWarehouse.longitude.HasValue &&
                destination.latitude.HasValue && destination.longitude.HasValue)
            {
                distanceKm = CalculateDistanceKm(
                    (double)originWarehouse.latitude.Value,
                    (double)originWarehouse.longitude.Value,
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
                originAddressId: originWarehouse.id,
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

    private async Task<Address?> FindNearestWarehouseAsync(Address sellerAddress, CancellationToken ct)
    {
        var warehouses = await _db.Address
            .AsNoTracking()
            .Where(x => x.userId == null
                && x.fullName != null
                && x.fullName.StartsWith("WAREHOUSE"))
            .ToListAsync(ct);

        if (!warehouses.Any())
            return null;

        // Ưu tiên 1: có tọa độ => tính khoảng cách thật
        if (sellerAddress.latitude.HasValue && sellerAddress.longitude.HasValue)
        {
            var warehousesWithCoordinates = warehouses
                .Where(w => w.latitude.HasValue && w.longitude.HasValue)
                .ToList();

            if (warehousesWithCoordinates.Any())
            {
                return warehousesWithCoordinates
                    .OrderBy(w => CalculateDistanceKm(
    (double)sellerAddress.latitude.Value,
    (double)sellerAddress.longitude.Value,
    (double)w.latitude!.Value,
    (double)w.longitude!.Value))
                    .FirstOrDefault();
            }
        }

        // Ưu tiên 2: không có tọa độ => fallback sang text
        var sellerCountry = NormalizeLocationText(sellerAddress.country);
        var sellerState = NormalizeLocationText(sellerAddress.state);
        var sellerCity = NormalizeLocationText(sellerAddress.city);

        var bestMatch = warehouses
            .Select(w => new
            {
                Warehouse = w,
                Score = CalculateWarehouseMatchScore(
                    sellerCountry,
                    sellerState,
                    sellerCity,
                    NormalizeLocationText(w.country),
                    NormalizeLocationText(w.state),
                    NormalizeLocationText(w.city),
                    NormalizeLocationText(w.street),
                    NormalizeLocationText(w.fullName)
                )
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Warehouse.id)
            .FirstOrDefault();

        return bestMatch?.Warehouse ?? warehouses.FirstOrDefault();
    }

    private static int CalculateWarehouseMatchScore(
        string sellerCountry,
        string sellerState,
        string sellerCity,
        string warehouseCountry,
        string warehouseState,
        string warehouseCity,
        string warehouseStreet,
        string warehouseFullName)
    {
        int score = 0;

        // cùng quốc gia
        if (!string.IsNullOrWhiteSpace(sellerCountry) &&
            sellerCountry == warehouseCountry)
        {
            score += 20;
        }

        // ưu tiên tỉnh/thành phố
        if (!string.IsNullOrWhiteSpace(sellerState))
        {
            if (sellerState == warehouseState)
                score += 100;

            if (warehouseState.Contains(sellerState) || sellerState.Contains(warehouseState))
                score += 80;

            if (warehouseCity.Contains(sellerState) || sellerState.Contains(warehouseCity))
                score += 60;

            if (warehouseStreet.Contains(sellerState))
                score += 40;

            if (warehouseFullName.Contains(sellerState))
                score += 40;
        }

        // city chỉ là ưu tiên phụ vì dữ liệu của bạn đang không đồng nhất
        if (!string.IsNullOrWhiteSpace(sellerCity))
        {
            if (sellerCity == warehouseCity)
                score += 50;

            if (warehouseCity.Contains(sellerCity) || sellerCity.Contains(warehouseCity))
                score += 35;

            if (warehouseState.Contains(sellerCity) || sellerCity.Contains(warehouseState))
                score += 30;

            if (warehouseStreet.Contains(sellerCity))
                score += 20;

            if (warehouseFullName.Contains(sellerCity))
                score += 20;
        }

        return score;
    }

    private static string NormalizeLocationText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToLowerInvariant();

        // bỏ dấu tiếng Việt
        value = value
            .Replace("đ", "d")
            .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
            .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
            .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
            .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
            .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
            .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
            .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
            .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
            .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
            .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
            .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
            .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");

        // chuẩn hóa tên địa danh hay gặp
        value = value
            .Replace("hanoi", "ha noi")
            .Replace("ha noi city", "ha noi")
            .Replace("ha noi - hanoi city", "ha noi")
            .Replace("thanh pho ha noi", "ha noi")
            .Replace("ho chi minh", "hcm")
            .Replace("ho chi minh city", "hcm")
            .Replace("thanh pho ho chi minh", "hcm")
            .Replace("sai gon", "hcm")
            .Replace("tp hcm", "hcm")
            .Replace("tp.hcm", "hcm")
            .Replace("da nang", "da nang")
            .Replace("danang", "da nang");

        // bỏ khoảng trắng thừa
        while (value.Contains("  "))
            value = value.Replace("  ", " ");

        return value.Trim();
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