using System;

namespace CloneEbay.Domain.Entities;

public partial class ShippingInfo
{
    public int id { get; set; }
    public int? orderId { get; set; }

    public string? carrier { get; set; }
    public string? trackingNumber { get; set; }
    public string? status { get; set; }
    public DateTime? estimatedArrival { get; set; }

    public DateTime? shippedAt { get; set; }
    public DateTime? deliveredAt { get; set; }

    public string? provider { get; set; }
    public string? providerTrackingId { get; set; }
    public DateTime? lastSyncedAt { get; set; }
    public string? lastCheckpoint { get; set; }
    public DateTime? lastCheckpointTime { get; set; }
    public string? rawLastPayload { get; set; }

    public virtual OrderTable? order { get; set; }
    public virtual ICollection<ShippingTrackingEvent> ShippingTrackingEvent { get; set; } = new List<ShippingTrackingEvent>();
}