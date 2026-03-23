namespace CloneEbay.Domain.Entities;

public partial class User
{
    public virtual SellerWallet? SellerWallet { get; set; }
    public virtual SellerTrustProfile? SellerTrustProfile { get; set; }
    public virtual ICollection<SellerSettlement> SellerSettlement { get; set; } = new List<SellerSettlement>();
    public virtual ICollection<UserCoupon> UserCoupon { get; set; } = new List<UserCoupon>();
}