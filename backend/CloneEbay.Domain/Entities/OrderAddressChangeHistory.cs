namespace CloneEbay.Domain.Entities;

public partial class OrderAddressChangeHistory
{
    public int id { get; set; }
    public int orderId { get; set; }
    public int oldAddressId { get; set; }
    public int newAddressId { get; set; }
    public int changedByUserId { get; set; }
    public string? reason { get; set; }
    public DateTime changedAt { get; set; }

    public virtual OrderTable? order { get; set; }
    public virtual Address? oldAddress { get; set; }
    public virtual Address? newAddress { get; set; }
    public virtual User? changedByUser { get; set; }
}