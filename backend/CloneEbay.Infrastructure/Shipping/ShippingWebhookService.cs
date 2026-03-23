using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Common.Diagnostics;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Shipping;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class ShippingWebhookService : IShippingWebhookService
{
    private readonly CloneEbayDbContext _db;
    private readonly IShippingService _shippingService;
    private readonly SeventeenTrackOptions _options;
    private readonly ILogger<ShippingWebhookService> _logger;
    private readonly ITransactionContextAccessor _txContext;

    public ShippingWebhookService(
        CloneEbayDbContext db,
        IShippingService shippingService,
        IOptions<SeventeenTrackOptions> options,
        ILogger<ShippingWebhookService> logger,
        ITransactionContextAccessor txContext)
    {
        _db = db;
        _shippingService = shippingService;
        _options = options.Value;
        _logger = logger;
        _txContext = txContext;
    }

    public async Task Handle17TrackWebhookAsync(
        string rawBody,
        string? signature,
        SeventeenTrackWebhookRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "17TRACK webhook received | cid={cid} | tx={tx} | eventType={eventType} | signaturePresent={hasSignature}",
            _txContext.CorrelationId,
            _txContext.TransactionId,
            request.eventType,
            !string.IsNullOrWhiteSpace(signature));

        var log = new ShippingWebhookEvent
        {
            provider = "17TRACK",
            eventType = request.eventType,
            trackingNumber = request.data?.FirstOrDefault()?.number,
            tag = request.data?.FirstOrDefault()?.tag,
            signature = signature,
            payload = rawBody,
            isProcessed = false,
            processedAt = null,
            createdAt = DateTime.UtcNow
        };

        _db.ShippingWebhookEvent.Add(log);
        await _db.SaveChangesAsync(ct);

        if (!VerifySignature(rawBody, signature))
        {
            _logger.LogWarning(
                "17TRACK webhook invalid signature | cid={cid} | tx={tx} | trackingNumber={trackingNumber}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                log.trackingNumber);

            return;
        }

        if (request.data == null || request.data.Count == 0)
        {
            log.isProcessed = true;
            log.processedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "17TRACK webhook empty data | cid={cid} | tx={tx}",
                _txContext.CorrelationId,
                _txContext.TransactionId);

            return;
        }

        foreach (var item in request.data)
        {
            var latest = item.track?.origin_info?
                .OrderByDescending(x => x.event_time)
                .FirstOrDefault();

            var eventLocation = latest?.location;
            if (string.IsNullOrWhiteSpace(eventLocation) || string.Equals(eventLocation.Trim(), "Destination", StringComparison.OrdinalIgnoreCase))
            {
                var parts = new List<string?>();
                if (!string.IsNullOrWhiteSpace(latest?.address)) parts.Add(latest.address);
                if (!string.IsNullOrWhiteSpace(latest?.city)) parts.Add(latest.city);
                if (!string.IsNullOrWhiteSpace(latest?.country)) parts.Add(latest.country);

                if (parts.Any())
                {
                    eventLocation = string.Join(", ", parts);
                }
            }

            _logger.LogInformation(
                "Applying tracking update | cid={cid} | tx={tx} | trackingNumber={trackingNumber} | tag={tag} | mainStatus={mainStatus} | subStatus={subStatus}",
                _txContext.CorrelationId,
                _txContext.TransactionId,
                item.number,
                item.tag,
                item.track?.package_status?.status,
                item.track?.package_status?.sub_status);

            await _shippingService.ApplyTrackingUpdateAsync(
                trackingNumber: item.number ?? "",
                tag: item.tag,
                mainStatus: item.track?.package_status?.status,
                subStatus: item.track?.package_status?.sub_status,
                description: latest?.description,
                location: eventLocation,
                eventTime: latest?.event_time,
                rawPayload: JsonSerializer.Serialize(item),
                ct: ct);
        }

        log.isProcessed = true;
        log.processedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "17TRACK webhook processed | cid={cid} | tx={tx} | trackingNumber={trackingNumber}",
            _txContext.CorrelationId,
            _txContext.TransactionId,
            log.trackingNumber);
    }

    private bool VerifySignature(string rawBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            return true;

        if (string.IsNullOrWhiteSpace(signature))
            return false;

        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawBody + _options.WebhookSecret);
        var hash = sha.ComputeHash(bytes);
        var computed = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(computed, signature.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }
}