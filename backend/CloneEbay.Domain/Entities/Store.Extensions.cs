namespace CloneEbay.Domain.Entities;

public partial class Store
{
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}