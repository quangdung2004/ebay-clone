using System.Net.Http.Json;
using CloneEbay.Application.Common.Diagnostics;
using CloneEbay.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CloneEbay.Infrastructure.Shipping;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public NominatimGeocodingService(
        HttpClient httpClient,
        ILogger<NominatimGeocodingService> logger,
        ITransactionContextAccessor txContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _txContext = txContext;

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

        if (keyword.Length < 4 || (keyword == keyword.ToUpperInvariant() && !keyword.Contains(' ')))
            return null;

        try
        {
            var encodedQuery = Uri.EscapeDataString(locationText);
            var url = $"https://nominatim.openstreetmap.org/search?q={encodedQuery}&format=jsonv2&limit=1";

            _logger.LogInformation(
                "Geocoding started | cid={cid} | tx={tx} | keyword={keyword}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                locationText);

            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Geocoding failed with status | cid={cid} | tx={tx} | keyword={keyword} | status={status}",
                    _txContext.CorrelationId,
                    _txContext.TransactionId,
                    locationText,
                    (int)response.StatusCode);

                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<List<NominatimResponse>>(cancellationToken: ct);
            if (result == null || result.Count == 0)
            {
                _logger.LogInformation(
                    "Geocoding no result | cid={cid} | tx={tx} | keyword={keyword}",
                    _txContext.CorrelationId,
                    _txContext.TransactionId,
                    locationText);

                return null;
            }

            var firstMatch = result[0];
            if (decimal.TryParse(firstMatch.lat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                decimal.TryParse(firstMatch.lon, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                _logger.LogInformation(
                    "Geocoding succeeded | cid={cid} | tx={tx} | keyword={keyword} | lat={lat} | lon={lon}",
                    _txContext.CorrelationId,
                    _txContext.TransactionId,
                    locationText,
                    lat,
                    lon);

                return (lat, lon);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Geocoding exception | cid={cid} | tx={tx} | keyword={keyword}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                locationText);
        }

        return null;
    }

    private class NominatimResponse
    {
        public string lat { get; set; } = "";
        public string lon { get; set; } = "";
    }
}