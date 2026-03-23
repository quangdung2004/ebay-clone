using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Common.Diagnostics;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Shipping;
using CloneEbay.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class SeventeenTrackClient : ISeventeenTrackClient
{
    private readonly HttpClient _http;
    private readonly SeventeenTrackOptions _options;
    private readonly ILogger<SeventeenTrackClient> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public SeventeenTrackClient(
        HttpClient http,
        IOptions<SeventeenTrackOptions> options,
        ILogger<SeventeenTrackClient> logger,
        ITransactionContextAccessor txContext)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _txContext = txContext;
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

        _logger.LogInformation(
            "17TRACK register started | cid={cid} | tx={tx} | trackingNumber={trackingNumber}",
            _txContext.CorrelationId,
            _txContext.TransactionId,
            request.number);

        using var response = await _http.SendAsync(httpRequest, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "17TRACK register failed | cid={cid} | tx={tx} | trackingNumber={trackingNumber} | status={status} | body={body}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                request.number,
                (int)response.StatusCode,
                Truncate(json, 1200));

            throw new ValidationException($"17TRACK register failed: {json}", "SEVENTEENTRACK_REGISTER_FAILED");
        }

        _logger.LogInformation(
            "17TRACK register succeeded | cid={cid} | tx={tx} | trackingNumber={trackingNumber}",
            _txContext.CorrelationId,
            _txContext.TransactionId,
            request.number);

        return new Register17TrackResultDto(
            trackingNumber: request.number,
            success: true,
            message: "Registered successfully");
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}