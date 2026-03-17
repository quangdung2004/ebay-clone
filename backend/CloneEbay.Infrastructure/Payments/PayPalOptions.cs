namespace CloneEbay.Infrastructure.Payments;

public sealed class PayPalOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public bool UseSandbox { get; set; } = true;
    public string Currency { get; set; } = "USD";

    public string BaseUrl =>
        UseSandbox
            ? "https://api-m.sandbox.paypal.com"
            : "https://api-m.paypal.com";
}