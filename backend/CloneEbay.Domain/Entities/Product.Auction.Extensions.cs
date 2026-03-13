namespace CloneEbay.Domain.Entities;

public partial class Product
{
    public int? winnerUserId { get; set; }

    public int? auctionOrderId { get; set; }

    public virtual User? winnerUser { get; set; }

    public virtual OrderTable? auctionOrder { get; set; }
}