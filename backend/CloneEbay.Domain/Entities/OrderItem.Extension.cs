namespace CloneEbay.Domain.Entities;

public partial class OrderItem
{
    public virtual ICollection<SellerSettlement> SellerSettlement { get; set; } = new List<SellerSettlement>();

    public decimal lineTotal => (unitPrice ?? 0m) * (quantity ?? 0);
}