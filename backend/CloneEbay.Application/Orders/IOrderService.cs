using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Products;

namespace CloneEbay.Application.Orders;

public interface IOrderService
{
    Task<OrderDetailDto> CreateAsync(int buyerId, CreateOrderRequest req, CancellationToken ct);
    Task<PagedResponse<OrderSummaryDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct);
    Task<OrderDetailDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct);
    Task<OrderTrackingDto> GetTrackingAsync(int buyerId, int orderId, CancellationToken ct);
    Task<OrderDetailDto> UpdateAddressAsync(int buyerId, int orderId, int addressId, CancellationToken ct);
    Task<OrderDetailDto> CancelAsync(int buyerId, int orderId, CancellationToken ct);
    Task<ShipmentTrackingDto> UpdateShipmentTrackingAsync(int sellerId, int shipmentId, UpdateShipmentTrackingRequest req, CancellationToken ct);
    Task<QuoteOrderDto> QuoteAsync(int buyerId, QuoteOrderRequest req, CancellationToken ct);

    Task<ShipmentTrackingDto> ConfirmShipmentHandlingAsync(int sellerId, int shipmentId, ConfirmShipmentHandlingRequest req, CancellationToken ct);

    Task<PagedResponse<SellerShipmentSummaryDto>> GetSellerShipmentsAsync(
    int sellerId,
    string? status,
    int page,
    int pageSize,
    CancellationToken ct);
}