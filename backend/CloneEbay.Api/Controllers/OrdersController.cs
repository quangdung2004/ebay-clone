using CloneEbay.Application.Orders;
using CloneEbay.Application.Payments;
using CloneEbay.Contracts;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Payments;
using CloneEbay.Contracts.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Api.Controllers;

[Authorize]
[Route("api/orders")]
public class OrdersController : BaseController
{
    private readonly IOrderService _svc;
    private readonly IPaymentService _paymentSvc;

    public OrdersController(IOrderService svc, IPaymentService paymentSvc)
    {
        _svc = svc;
        _paymentSvc = paymentSvc;
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
    public async Task<ApiResponse<CreatePayPalPaymentDto>> Pay([FromRoute] int id, CancellationToken ct)
        => Success(await _paymentSvc.CreatePayPalOrderAsync(CurrentUserId, id, ct), "Create PayPal order successfully", "ORDER_PAYPAL_CREATE_SUCCESS");

    [HttpPost("{id:int}/pay/capture")]
    public async Task<ApiResponse<OrderDetailDto>> CapturePay(
        [FromRoute] int id,
        [FromBody] CapturePayPalPaymentRequest req,
        CancellationToken ct)
        => Success(await _paymentSvc.CapturePayPalOrderAsync(CurrentUserId, id, req.paypalOrderId, ct), "Pay order successfully", "ORDER_PAY_SUCCESS");

    [HttpPost("{id:int}/cancel")]
    public async Task<ApiResponse<OrderDetailDto>> Cancel([FromRoute] int id, CancellationToken ct)
        => Success(await _svc.CancelAsync(CurrentUserId, id, ct), "Cancel order successfully", "ORDER_CANCEL_SUCCESS");

    [HttpPost("checkout/preview")]
    public async Task<ApiResponse<OrderPricePreviewDto>> PreviewCheckout([FromBody] CreateOrderRequest req, CancellationToken ct)
    => Success(await _svc.PreviewAsync(CurrentUserId, req, ct), "Preview order successfully", "ORDER_PREVIEW_SUCCESS");

    [HttpGet("my-coupons")]
    public async Task<ApiResponse<IReadOnlyList<MyCouponDto>>> GetMyCoupons(CancellationToken ct)
    => Success(await _svc.GetMyCouponsAsync(CurrentUserId, ct), "Get coupons successfully", "COUPON_LIST_SUCCESS");
}