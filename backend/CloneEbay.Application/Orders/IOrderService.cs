using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Products;

namespace CloneEbay.Application.Orders;

public interface IOrderService
{
    Task<OrderDetailDto> CreateAsync(int buyerId, CreateOrderRequest req, CancellationToken ct);
    Task<PagedResponse<OrderSummaryDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct);
    Task<OrderDetailDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct);
    Task<OrderDetailDto> UpdateAddressAsync(int buyerId, int orderId, int addressId, CancellationToken ct);
    Task<OrderDetailDto> PayAsync(int buyerId, int orderId, CancellationToken ct);
    Task<OrderDetailDto> CancelAsync(int buyerId, int orderId, CancellationToken ct);
}