using System.Text.Json;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Infrastructure.Products;

public static class ProductImageJson
{
    public static List<string> Read(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.images)) return new();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(p.images!)?
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                       .Distinct(StringComparer.OrdinalIgnoreCase)
                       .ToList()
                   ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void Write(Product p, IEnumerable<string> urls)
    {
        var normalized = urls
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        p.images = JsonSerializer.Serialize(normalized);
    }
}