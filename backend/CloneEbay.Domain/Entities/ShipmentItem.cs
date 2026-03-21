namespace CloneEbay.Domain.Entities;

public partial class ShipmentItem
{
    public int id { get; set; }

    public int shipmentId { get; set; }

    public int orderItemId { get; set; }

    public int quantity { get; set; }

    public virtual Shipment? shipment { get; set; }

    public virtual OrderItem? orderItem { get; set; }
}