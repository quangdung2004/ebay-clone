using CloneEbay.Contracts.Shipping;

namespace CloneEbay.Application.Shipping;

public interface IShippingWebhookService
{
    Task Handle17TrackWebhookAsync(string rawBody, string? signature, SeventeenTrackWebhookRequest request, CancellationToken ct);
}