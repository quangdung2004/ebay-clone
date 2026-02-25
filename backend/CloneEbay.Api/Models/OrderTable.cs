using System;
using System.Collections.Generic;

namespace CloneEbay.Api.Models;

public partial class OrderTable
{
    public int id { get; set; }

    public int? buyerId { get; set; }

    public int? addressId { get; set; }

    public DateTime? orderDate { get; set; }

    public decimal? totalPrice { get; set; }

    public string? status { get; set; }

    public virtual ICollection<Dispute> Dispute { get; set; } = new List<Dispute>();

    public virtual ICollection<OrderItem> OrderItem { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();

    public virtual ICollection<ReturnRequest> ReturnRequest { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<ShippingInfo> ShippingInfo { get; set; } = new List<ShippingInfo>();

    public virtual Address? address { get; set; }

    public virtual User? buyer { get; set; }
}
