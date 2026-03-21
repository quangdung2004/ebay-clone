using System;
using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class Shipment
{
    public int id { get; set; }

    public int orderId { get; set; }

    public int sellerId { get; set; }

    public int originAddressId { get; set; }

    public int destinationAddressId { get; set; }

    public string? shippingMethod { get; set; }

    public string? carrier { get; set; }

    public string? trackingNumber { get; set; }

    public string? status { get; set; }

    public decimal? shippingCost { get; set; }

    public string? currency { get; set; }

    public DateTime? estimatedShipDate { get; set; }

    public DateTime? estimatedDeliveryDate { get; set; }

    public DateTime? shippedAt { get; set; }

    public DateTime? deliveredAt { get; set; }

    public DateTime? createdAt { get; set; }

    public virtual OrderTable? order { get; set; }

    public virtual User? seller { get; set; }

    public virtual Address? originAddress { get; set; }

    public virtual Address? destinationAddress { get; set; }

    public virtual ICollection<ShipmentItem> ShipmentItem { get; set; } = new List<ShipmentItem>();

    public virtual ICollection<TrackingEvent> TrackingEvent { get; set; } = new List<TrackingEvent>();
}