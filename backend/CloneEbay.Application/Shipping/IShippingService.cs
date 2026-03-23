using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Shipping;

namespace CloneEbay.Application.Shipping;

public interface IShippingService
{
    Task<OrderDetailDto> MarkProcessingAsync(int sellerId, int orderId, CancellationToken ct);
    Task<OrderDetailDto> CreateShipmentAsync(int sellerId, int orderId, CreateShipmentRequest req, CancellationToken ct);
    Task ApplyTrackingUpdateAsync(string trackingNumber, string? tag, string? mainStatus, string? subStatus, string? description, string? location, DateTime? eventTime, string rawPayload, CancellationToken ct);
    Task<OrderDetailDto> ApplyMockStatusAsync(int sellerId, int orderId, MockShipmentStatusRequest req, CancellationToken ct);
    Task<OrderDetailDto> GetOrderDetailForSellerAsync(int orderId, int sellerId, CancellationToken ct);
}