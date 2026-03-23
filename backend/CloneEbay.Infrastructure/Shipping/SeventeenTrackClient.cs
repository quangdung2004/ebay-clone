using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Shipping;
using CloneEbay.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class SeventeenTrackClient : ISeventeenTrackClient
{
    private readonly HttpClient _http;
    private readonly SeventeenTrackOptions _options;

    public SeventeenTrackClient(HttpClient http, IOptions<SeventeenTrackOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<Register17TrackResultDto> RegisterTrackingAsync(Register17TrackRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.number))
            throw new ValidationException("Tracking number is required", "TRACKING_NUMBER_REQUIRED");

        var payload = new[]
        {
            new
            {
                number = request.number,
                carrier = request.carrier,
                tag = request.tag
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl.TrimEnd('/')}/register");

        httpRequest.Headers.Add("17token", _options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(httpRequest, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException($"17TRACK register failed: {json}", "SEVENTEENTRACK_REGISTER_FAILED");

        return new Register17TrackResultDto(
            trackingNumber: request.number,
            success: true,
            message: "Registered successfully");
    }
}