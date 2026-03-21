namespace CloneEbay.Domain.Entities;

public partial class OrderTable
{
    public virtual ICollection<SellerSettlement> SellerSettlement { get; set; } = new List<SellerSettlement>();
}