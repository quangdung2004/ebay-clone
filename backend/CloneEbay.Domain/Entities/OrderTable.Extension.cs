namespace CloneEbay.Domain.Entities;

public partial class OrderTable
{
    public virtual ICollection<SellerSettlement> SellerSettlement { get; set; } = new List<SellerSettlement>();

    public decimal? subtotalAmount { get; set; }
    public decimal? shippingFee { get; set; }

    public string? couponCode { get; set; }
    public decimal? discountAmount { get; set; }

    public int addressChangeCount { get; set; }
    public DateTime? lastAddressChangedAt { get; set; }

    public virtual ICollection<OrderAddressChangeHistory> OrderAddressChangeHistory { get; set; } = new List<OrderAddressChangeHistory>();
}