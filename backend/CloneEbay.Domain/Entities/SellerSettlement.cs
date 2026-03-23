namespace CloneEbay.Domain.Entities;

public partial class SellerSettlement
{
    public int id { get; set; }

    public int orderId { get; set; }
    public int orderItemId { get; set; }
    public int sellerId { get; set; }

    public decimal grossAmount { get; set; }
    public decimal platformFee { get; set; }
    public decimal netAmount { get; set; }

    public string status { get; set; } = "PENDING";
    public string? holdReason { get; set; }

    public DateTime? heldAt { get; set; }
    public DateTime? availableAt { get; set; }
    public DateTime? releasedAt { get; set; }

    public virtual OrderTable? order { get; set; }
    public virtual OrderItem? orderItem { get; set; }
    public virtual User? seller { get; set; }
}