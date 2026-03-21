namespace CloneEbay.Domain.Entities;

public partial class SellerWallet
{
    public int id { get; set; }
    public int sellerId { get; set; }

    public decimal pendingBalance { get; set; }
    public decimal availableBalance { get; set; }
    public decimal totalEarned { get; set; }

    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }

    public virtual User? seller { get; set; }
}