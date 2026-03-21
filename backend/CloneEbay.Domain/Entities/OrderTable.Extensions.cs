using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class OrderTable
{
    public decimal? itemSubtotal { get; set; }

    public decimal? shippingTotal { get; set; }

    public decimal? discountTotal { get; set; }

    public decimal? taxTotal { get; set; }

    public decimal? grandTotal { get; set; }

    public virtual ICollection<Shipment> Shipment { get; set; } = new List<Shipment>();
}