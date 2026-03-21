namespace CloneEbay.Domain.Entities;

public partial class SellerTrustProfile
{
    public int id { get; set; }
    public int sellerId { get; set; }

    public int level { get; set; } = 1;
    public int completedOrders { get; set; }
    public int successfulDeliveries { get; set; }
    public int refundCount { get; set; }
    public int disputeCount { get; set; }
    public bool isVerified { get; set; }

    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }

    public virtual User? seller { get; set; }
}