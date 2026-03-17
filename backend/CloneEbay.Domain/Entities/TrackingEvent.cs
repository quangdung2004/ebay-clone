using System;

namespace CloneEbay.Domain.Entities;

public partial class TrackingEvent
{
    public int id { get; set; }

    public int shipmentId { get; set; }

    public string? statusCode { get; set; }

    public string? description { get; set; }

    public string? location { get; set; }

    public DateTime? eventTime { get; set; }

    public virtual Shipment? shipment { get; set; }
}