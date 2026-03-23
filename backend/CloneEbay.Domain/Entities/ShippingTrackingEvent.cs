namespace CloneEbay.Domain.Entities;

public partial class ShippingTrackingEvent
{
    public int id { get; set; }
    public int shippingInfoId { get; set; }
    public string provider { get; set; } = "17TRACK";
    public string trackingNumber { get; set; } = "";
    public string? mainStatus { get; set; }
    public string? subStatus { get; set; }
    public string? description { get; set; }
    public string? location { get; set; }
    public DateTime? eventTime { get; set; }
    public string? rawPayload { get; set; }
    public DateTime createdAt { get; set; }

    public decimal? latitude { get; set; }
    public decimal? longitude { get; set; }
    public string? normalizedLocation { get; set; }
    public string? geocodeStatus { get; set; }

    public virtual ShippingInfo? shippingInfo { get; set; }
}