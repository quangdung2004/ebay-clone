using System.Globalization;
using System.Linq;
using CloneEbay.Application.Notifications;
using CloneEbay.Application.Orders;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Orders;

public sealed class OrderEmailService : IOrderEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly AuthOptions _options;

    public OrderEmailService(IEmailSender emailSender, IOptions<AuthOptions> options)
    {
        _emailSender = emailSender;
        _options = options.Value;
    }

    public async Task SendPaymentSuccessEmailAsync(OrderTable order, CancellationToken ct)
    {
        if (order?.buyer?.email == null) return;

        var subtotal = order.subtotalAmount ?? 0m;
        var shipping = order.shippingFee ?? 0m;
        var total = order.totalPrice ?? (subtotal + shipping);

        var subject = $"Payment Success for Order #{order.id}";
        var body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;"">
    <h2 style=""color: #0053a0;"">Payment Confirmed!</h2>
    <p>Hello <strong>{order.address?.fullName ?? order.buyer?.username ?? "Valued Customer"}</strong>,</p>
    <p>Your payment for order <strong>#{order.id}</strong> has been successfully captured. We are now processing your shipment.</p>
    
    <table style=""width: 100%; border-collapse: collapse; margin-top: 20px;"">
        <thead>
            <tr style=""background-color: #f3f4f6;"">
                <th style=""text-align: left; padding: 10px; border-bottom: 2px solid #e5e7eb;"">Item</th>
                <th style=""text-align: right; padding: 10px; border-bottom: 2px solid #e5e7eb;"">Price</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", order.OrderItem.Select(i => $@"
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #f3f4f6;"">{i.product?.title} (x{i.quantity})</td>
                <td style=""padding: 10px; border-bottom: 1px solid #f3f4f6; text-align: right;"">{i.lineTotal:C2}</td>
            </tr>"))}
        </tbody>
        <tfoot>
            <tr>
                <td style=""padding: 10px; text-align: right;"">Subtotal:</td>
                <td style=""padding: 10px; text-align: right;"">{subtotal:C2}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; text-align: right;"">Shipping:</td>
                <td style=""padding: 10px; text-align: right;"">{shipping:C2}</td>
            </tr>
            <tr style=""font-weight: bold; font-size: 1.1rem;"">
                <td style=""padding: 10px; text-align: right;"">Total Paid:</td>
                <td style=""padding: 10px; text-align: right; color: #0053a0;"">{total:C2}</td>
            </tr>
        </tfoot>
    </table>

    <p style=""margin-top: 30px;"">
        <a href=""{_options.FrontendBaseUrl}/orders/{order.id}"" style=""background-color: #0053a0; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">View Order Details</a>
    </p>
    <p style=""color: #6b7280; font-size: 0.9rem;"">Thank you for shopping at CloneEbay!</p>
</div>";

        await _emailSender.SendAsync(order.buyer.email, subject, body, ct);
    }

    public async Task SendOrderStatusChangedEmailAsync(OrderTable order, string oldStatus, string newStatus, CancellationToken ct)
    {
        if (order?.buyer?.email == null) return;

        var statusDisplay = newStatus.Replace("_", " ").ToUpperInvariant();
        var subject = $"Update for Order #{order.id}: {statusDisplay}";
        
        string message;
        string ctaLabel = "Track Order";

        switch (newStatus.ToUpperInvariant())
        {
            case "SHIPPED":
                message = "Good news! Your package has been handed over to the carrier and is on its way.";
                break;
            case "DELIVERED":
                message = "Your package has been delivered! We hope you enjoy your purchase.";
                ctaLabel = "View Order";
                break;
            case "CANCELLED":
                message = "Your order has been cancelled. If this was a mistake, please contact support.";
                ctaLabel = "Go to Shop";
                break;
            default:
                message = $"The status of your order #{order.id} has changed from {oldStatus} to {newStatus}.";
                break;
        }

        var body = $@"
<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 8px;"">
    <h2 style=""color: #0053a0;"">Order Update: {statusDisplay}</h2>
    <p>Hello <strong>{order.address?.fullName ?? order.buyer?.username ?? "Valued Customer"}</strong>,</p>
    <p>{message}</p>
    
    <div style=""background-color: #f9fafb; padding: 15px; border-radius: 4px; margin: 20px 0;"">
        <p style=""margin: 0;"">Order ID: <strong>#{order.id}</strong></p>
        <p style=""margin: 5px 0 0 0;"">New Status: <span style=""color: #059669; font-weight: bold;"">{statusDisplay}</span></p>
    </div>

    <p style=""margin-top: 30px;"">
        <a href=""{_options.FrontendBaseUrl}/orders/{order.id}"" style=""background-color: #0053a0; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">{ctaLabel}</a>
    </p>
    <p style=""color: #6b7280; font-size: 0.8rem; margin-top: 40px; border-top: 1px solid #e5e7eb; padding-top: 20px;"">
        If you have any questions, please reply to this email.
    </p>
</div>";

        await _emailSender.SendAsync(order.buyer.email, subject, body, ct);
    }
}
