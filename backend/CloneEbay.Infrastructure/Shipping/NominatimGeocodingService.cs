using System.Net.Http.Json;
using CloneEbay.Application.Common.Interfaces;

namespace CloneEbay.Infrastructure.Shipping;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;

    public NominatimGeocodingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Nominatim requests User-Agent header
        if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("CloneEbayTracking/1.0"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CloneEbayTracking/1.0");
        }
    }

    public async Task<(decimal latitude, decimal longitude)?> GeocodeLocationAsync(string locationText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locationText))
            return null;

        var keyword = locationText.Trim();

        // These are generic logistics descriptions, not real place names.
        // Nominatim will return random global matches for them → filter out.
        var genericTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Destination",
            "Origin",
            "Local Delivery Office",
            "Sorting Center",
            "Sorting Centre",
            "Distribution Center",
            "Distribution Centre",
            "Delivery Center",
            "Delivery Centre",
            "Hub",
            "Depot",
            "Warehouse",
            "Customs",
            "Customs Clearance",
            "Transit Hub",
            "Collection Point",
            "Post Office",
            "Facility",
            "Carrier Facility",
            "In Transit",
            "In-Transit",
            "Out for Delivery",
            "Delivered",
            "Picked Up",
        };

        if (genericTerms.Contains(keyword))
            return null;

        // Also skip if the text is just 1-2 words and all uppercase (e.g. "HUB", "SORT CTR")
        if (keyword.Length < 4 || (keyword == keyword.ToUpperInvariant() && !keyword.Contains(' ')))
            return null;


        try
        {
            // Prepare the URL
            var encodedQuery = Uri.EscapeDataString(locationText);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=jsonv2&limit=1";

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<List<NominatimResponse>>(cancellationToken: ct);
            if (result == null || result.Count == 0)
                return null;

            var firstMatch = result[0];
            if (decimal.TryParse(firstMatch.lat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) && 
                decimal.TryParse(firstMatch.lon, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }
        }
        catch
        {
            // Log error
        }

        return null;
    }

    private class NominatimResponse
    {
        public string lat { get; set; } = "";
        public string lon { get; set; } = "";
    }
}
