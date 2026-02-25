using System;
using System.Collections.Generic;

namespace CloneEbay.Api.Models;

public partial class Product
{
    public int id { get; set; }

    public string? title { get; set; }

    public string? description { get; set; }

    public decimal? price { get; set; }

    public string? images { get; set; }

    public int? categoryId { get; set; }

    public int? sellerId { get; set; }

    public bool? isAuction { get; set; }

    public DateTime? auctionEndTime { get; set; }

    public virtual ICollection<Bid> Bid { get; set; } = new List<Bid>();

    public virtual ICollection<Coupon> Coupon { get; set; } = new List<Coupon>();

    public virtual ICollection<Inventory> Inventory { get; set; } = new List<Inventory>();

    public virtual ICollection<OrderItem> OrderItem { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Review { get; set; } = new List<Review>();

    public virtual Category? category { get; set; }

    public virtual User? seller { get; set; }
}
