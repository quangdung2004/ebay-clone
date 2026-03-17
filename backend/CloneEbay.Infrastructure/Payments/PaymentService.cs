using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Payments;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Payments;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly CloneEbayDbContext _db;
    private readonly HttpClient _http;
    private readonly PayPalOptions _options;

    private static class OrderStatuses
    {
        public const string PendingPayment = "PENDING_PAYMENT";
        public const string Confirmed = "CONFIRMED";
        public const string Paid = "PAID";
        public const string Cancelled = "CANCELLED";
    }

    private static class PaymentStatuses
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Cancelled = "CANCELLED";
    }

    public PaymentService(
        CloneEbayDbContext db,
        HttpClient http,
        IOptions<PayPalOptions> options)
    {
        _db = db;
        _http = http;
        _options = options.Value;
    }

    public async Task<CreatePayPalPaymentDto> CreatePayPalOrderAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to pay this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be paid", "ORDER_ALREADY_CANCELLED");

        if (order.addressId == null)
            throw new ValidationException("Order must have a shipping address before payment", "ORDER_ADDRESS_REQUIRED");

        var payment = order.Payment.OrderByDescending(x => x.id).FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        if (!string.Equals(payment.method, "PAYPAL", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("This order does not use PayPal", "PAYMENT_METHOD_NOT_PAYPAL");

        if (string.Equals(payment.status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Order has already been paid", "ORDER_ALREADY_PAID");

        var accessToken = await GetAccessTokenAsync(ct);

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
    {
        new
        {
            reference_id = order.id.ToString(),
            amount = new
            {
                currency_code = _options.Currency,
                value = (order.totalPrice ?? 0m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            }
        }
    },
            application_context = new
            {
                shipping_preference = "NO_SHIPPING",
                user_action = "PAY_NOW"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException("Failed to create PayPal order", "PAYPAL_CREATE_ORDER_FAILED");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var paypalOrderId = root.GetProperty("id").GetString()
            ?? throw new ValidationException("Invalid PayPal response", "PAYPAL_INVALID_RESPONSE");

        var status = root.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString() ?? "UNKNOWN"
            : "UNKNOWN";

        string? approveUrl = null;
        if (root.TryGetProperty("links", out var linksEl) && linksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in linksEl.EnumerateArray())
            {
                var rel = link.TryGetProperty("rel", out var relEl) ? relEl.GetString() : null;
                if (string.Equals(rel, "approve", StringComparison.OrdinalIgnoreCase))
                {
                    approveUrl = link.TryGetProperty("href", out var hrefEl) ? hrefEl.GetString() : null;
                    break;
                }
            }
        }

        return new CreatePayPalPaymentDto(
            paypalOrderId: paypalOrderId,
            status: status,
            approveUrl: approveUrl
        );
    }

    public async Task<OrderDetailDto> CapturePayPalOrderAsync(int buyerId, int orderId, string paypalOrderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paypalOrderId))
            throw new ValidationException("paypalOrderId is required", "PAYPAL_ORDER_ID_REQUIRED");

        var order = await _db.OrderTable
            .Include(x => x.buyer)
            .Include(x => x.address)
            .Include(x => x.OrderItem).ThenInclude(x => x.product).ThenInclude(x => x!.seller)
            .Include(x => x.Payment)
            .Include(x => x.ShippingInfo)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to pay this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be paid", "ORDER_ALREADY_CANCELLED");

        var payment = order.Payment.OrderByDescending(x => x.id).FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        if (!string.Equals(payment.method, "PAYPAL", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("This order does not use PayPal", "PAYMENT_METHOD_NOT_PAYPAL");

        if (string.Equals(payment.status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return MapOrderDetail(order);

        var accessToken = await GetAccessTokenAsync(ct);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException("Failed to capture PayPal order", "PAYPAL_CAPTURE_FAILED");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString()
            : null;

        if (!string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            // nhiều case thực tế capture thành công sẽ trả COMPLETED
            // nếu không an toàn thì coi là fail
            throw new ValidationException("PayPal payment was not completed", "PAYPAL_PAYMENT_NOT_COMPLETED");
        }

        payment.status = PaymentStatuses.Paid;
        payment.paidAt = DateTime.UtcNow;
        order.status = OrderStatuses.Paid;

        await _db.SaveChangesAsync(ct);

        return MapOrderDetail(order);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException("Failed to authenticate with PayPal", "PAYPAL_AUTH_FAILED");

        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("access_token").GetString();

        if (string.IsNullOrWhiteSpace(token))
            throw new ValidationException("PayPal access token is missing", "PAYPAL_ACCESS_TOKEN_MISSING");

        return token;
    }

    private static OrderDetailDto MapOrderDetail(OrderTable order)
    {
        return new OrderDetailDto(
            id: order.id,
            buyerId: order.buyerId,
            buyerName: order.buyer?.username,
            orderDate: order.orderDate,
            totalPrice: order.totalPrice ?? 0m,
            status: order.status,
            address: order.address == null ? null : new AddressSummaryDto(
                order.address.id,
                order.address.fullName,
                order.address.phone,
                order.address.street,
                order.address.city,
                order.address.state,
                order.address.country,
                order.address.isDefault),
            items: order.OrderItem.Select(MapItem).ToList(),
            payments: order.Payment.Select(MapPayment).ToList(),
            shippings: order.ShippingInfo.Select(x => new ShippingSummaryDto(
                x.id,
                x.carrier,
                x.trackingNumber,
                x.status,
                x.estimatedArrival)).ToList()
        );
    }

    private static OrderItemSummaryDto MapItem(OrderItem item)
    {
        var image = item.product == null ? null : ProductImageJson.Read(item.product).FirstOrDefault();

        return new OrderItemSummaryDto(
            id: item.id,
            productId: item.productId,
            productTitle: item.product?.title ?? "Unknown product",
            thumbnailUrl: image,
            quantity: item.quantity ?? 0,
            unitPrice: item.unitPrice ?? 0m,
            lineTotal: (item.unitPrice ?? 0m) * (item.quantity ?? 0),
            sellerId: item.product?.sellerId,
            sellerName: item.product?.seller?.username
        );
    }

    private static PaymentSummaryDto MapPayment(Payment payment)
    {
        return new PaymentSummaryDto(
            id: payment.id,
            amount: payment.amount ?? 0m,
            method: payment.method,
            status: payment.status,
            paidAt: payment.paidAt
        );
    }
}