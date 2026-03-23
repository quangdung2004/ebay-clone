using CloneEbay.Application.Shipping;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloneEbay.Contracts.Products;

namespace CloneEbay.Api.Controllers;

[Authorize]
[Route("api/seller/orders")]
public sealed class SellerShippingController : BaseController
{
    private readonly IShippingService _shippingService;
    private readonly CloneEbay.Application.Orders.IOrderService _orderService;

    public SellerShippingController(IShippingService shippingService, CloneEbay.Application.Orders.IOrderService orderService)
    {
        _shippingService = shippingService;
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ApiResponse<PagedResponse<OrderSummaryDto>>> GetSellerOrdersAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Success(
            await _orderService.GetSellerOrdersAsync(CurrentUserId, page, pageSize, ct),
            "Get seller orders successfully",
            "SELLER_ORDERS_GET_SUCCESS");

    [HttpGet("{orderId:int}")]
    public async Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync([FromRoute] int orderId, CancellationToken ct)
        => Success(
            await _shippingService.GetOrderDetailForSellerAsync(orderId, CurrentUserId, ct),
            "Get seller order details successfully",
            "SELLER_ORDER_DETAIL_SUCCESS");

    [HttpPost("{orderId:int}/processing")]
    public async Task<ApiResponse<OrderDetailDto>> MarkProcessing([FromRoute] int orderId, CancellationToken ct)
        => Success(
            await _shippingService.MarkProcessingAsync(CurrentUserId, orderId, ct),
            "Order moved to processing successfully",
            "ORDER_PROCESSING_SUCCESS");

    [HttpPost("{orderId:int}/ship")]
    public async Task<ApiResponse<OrderDetailDto>> Ship(
        [FromRoute] int orderId,
        [FromBody] CreateShipmentRequest req,
        CancellationToken ct)
        => Success(
            await _shippingService.CreateShipmentAsync(CurrentUserId, orderId, req, ct),
            "Shipment created successfully",
            "ORDER_SHIP_SUCCESS");

    [HttpPost("{orderId:int}/mock-status")]
    public async Task<ApiResponse<OrderDetailDto>> MockStatus(
        [FromRoute] int orderId,
        [FromBody] MockShipmentStatusRequest req,
        CancellationToken ct)
        => Success(
            await _shippingService.ApplyMockStatusAsync(CurrentUserId, orderId, req, ct),
            "Mock shipment status applied successfully",
            "MOCK_SHIPMENT_STATUS_SUCCESS");
}