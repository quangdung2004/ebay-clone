namespace CloneEbay.Infrastructure.Shipping;

public sealed class SeventeenTrackOptions
{
    public string BaseUrl { get; set; } = "https://api.17track.net/track/v2.2";
    public string ApiKey { get; set; } = "";
    public string? WebhookSecret { get; set; }
}