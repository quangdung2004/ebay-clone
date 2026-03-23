namespace CloneEbay.Domain.Entities;

public partial class ShippingWebhookEvent
{
    public int id { get; set; }
    public string provider { get; set; } = "17TRACK";
    public string? eventType { get; set; }
    public string? trackingNumber { get; set; }
    public string? tag { get; set; }
    public string? signature { get; set; }
    public string payload { get; set; } = "";
    public bool isProcessed { get; set; }
    public DateTime? processedAt { get; set; }
    public DateTime createdAt { get; set; }
}