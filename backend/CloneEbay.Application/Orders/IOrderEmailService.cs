using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Orders;

public interface IOrderEmailService
{
    /// <summary>
    /// Sends a confirmation email after a successful payment capture.
    /// </summary>
    Task SendPaymentSuccessEmailAsync(OrderTable order, CancellationToken ct);

    /// <summary>
    /// Sends an email when the status of an order changes (e.g., Shipped, Delivered, Cancelled).
    /// </summary>
    Task SendOrderStatusChangedEmailAsync(OrderTable order, string oldStatus, string newStatus, CancellationToken ct);
}
