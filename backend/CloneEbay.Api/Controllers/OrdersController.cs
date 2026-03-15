using CloneEbay.Application.Orders;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Authorize]
[Route("api/orders")]
public class OrdersController : BaseController
{
    private readonly IOrderService _svc;

    public OrdersController(IOrderService svc)
    {
        _svc = svc;
    }

    [HttpPost("checkout")]
    public async Task<ApiResponse<OrderDetailDto>> Checkout([FromBody] CreateOrderRequest req, CancellationToken ct)
        => Success(await _svc.CreateAsync(CurrentUserId, req, ct), "Create order successfully", "ORDER_CREATE_SUCCESS");

    [HttpGet("my")]
    public async Task<ApiResponse<PagedResponse<OrderSummaryDto>>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Success(await _svc.GetMyOrdersAsync(CurrentUserId, page, pageSize, ct), "Get orders successfully", "ORDER_LIST_SUCCESS");

    [HttpGet("{id:int}")]
    public async Task<ApiResponse<OrderDetailDto>> GetById([FromRoute] int id, CancellationToken ct)
        => Success(await _svc.GetByIdAsync(CurrentUserId, id, ct), "Get order successfully", "ORDER_DETAIL_SUCCESS");

    [HttpPut("{id:int}/address")]
    public async Task<ApiResponse<OrderDetailDto>> UpdateAddress([FromRoute] int id, [FromBody] UpdateOrderAddressRequest req, CancellationToken ct)
        => Success(await _svc.UpdateAddressAsync(CurrentUserId, id, req.addressId, ct), "Update order address successfully", "ORDER_ADDRESS_UPDATE_SUCCESS");

    [HttpPost("{id:int}/pay")]
    public async Task<ApiResponse<OrderDetailDto>> Pay([FromRoute] int id, CancellationToken ct)
        => Success(await _svc.PayAsync(CurrentUserId, id, ct), "Pay order successfully", "ORDER_PAY_SUCCESS");

    [HttpPost("{id:int}/cancel")]
    public async Task<ApiResponse<OrderDetailDto>> Cancel([FromRoute] int id, CancellationToken ct)
        => Success(await _svc.CancelAsync(CurrentUserId, id, ct), "Cancel order successfully", "ORDER_CANCEL_SUCCESS");
}