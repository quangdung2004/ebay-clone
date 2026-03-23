using System;
using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class UserCoupon
{
    public int id { get; set; }

    public int userId { get; set; }

    public int couponId { get; set; }

    public int quantity { get; set; }

    public DateTime? assignedAt { get; set; }

    public virtual User? user { get; set; }

    public virtual Coupon? coupon { get; set; }
}