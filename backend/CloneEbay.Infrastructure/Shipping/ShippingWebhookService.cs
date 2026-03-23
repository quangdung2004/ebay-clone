using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Shipping;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class ShippingWebhookService : IShippingWebhookService
{
    private readonly CloneEbayDbContext _db;
    private readonly IShippingService _shippingService;
    private readonly SeventeenTrackOptions _options;

    public ShippingWebhookService(
        CloneEbayDbContext db,
        IShippingService shippingService,
        IOptions<SeventeenTrackOptions> options)
    {
        _db = db;
        _shippingService = shippingService;
        _options = options.Value;
    }

    public async Task Handle17TrackWebhookAsync(
        string rawBody,
        string? signature,
        SeventeenTrackWebhookRequest request,
        CancellationToken ct)
    {
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
            return;

        if (request.data == null || request.data.Count == 0)
        {
            log.isProcessed = true;
            log.processedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
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
                // Fallback: build location from components
                var parts = new List<string?>();
                if (!string.IsNullOrWhiteSpace(latest?.address)) parts.Add(latest.address);
                if (!string.IsNullOrWhiteSpace(latest?.city)) parts.Add(latest.city);
                if (!string.IsNullOrWhiteSpace(latest?.country)) parts.Add(latest.country);
                
                if (parts.Any())
                {
                    eventLocation = string.Join(", ", parts);
                }
            }

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