using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Payments;

namespace CloneEbay.Application.Payments;

public interface IPaymentService
{
    Task<CreatePayPalPaymentDto> CreatePayPalOrderAsync(int buyerId, int orderId, CancellationToken ct);
    Task<OrderDetailDto> CapturePayPalOrderAsync(int buyerId, int orderId, string paypalOrderId, CancellationToken ct);
}