using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CloneEbay.Application.Common;
using CloneEbay.Application.Payments;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Payments;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Common.Helpers;
using CloneEbay.Infrastructure.Common.Mappers;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CloneEbay.Infrastructure.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly CloneEbayDbContext _db;
    private readonly HttpClient _http;
    private readonly PayPalOptions _options;
    private readonly ISellerHoldPolicyService _holdPolicy;
    private readonly CloneEbay.Application.Orders.IOrderEmailService _emailService;

    public PaymentService(
        CloneEbayDbContext db,
        HttpClient http,
        IOptions<PayPalOptions> options,
        ISellerHoldPolicyService holdPolicy,
        CloneEbay.Application.Orders.IOrderEmailService emailService)
    {
        _db = db;
        _http = http;
        _options = options.Value;
        _holdPolicy = holdPolicy;
        _emailService = emailService;
    }

    public async Task<CreatePayPalPaymentDto> CreatePayPalOrderAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        ValidateOrderBeforeCreate(order, buyerId);

        var payment = GetLatestPayment(order!);
        ValidatePayPalPayment(payment, allowCaptured: false);

        var accessToken = await GetAccessTokenAsync(ct);

        using var request = BuildCreateOrderRequest(order!, accessToken);
        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException("Failed to create PayPal order", "PAYPAL_CREATE_ORDER_FAILED");

        return ParseCreateOrderResponse(json);
    }

    public async Task<OrderDetailDto> CapturePayPalOrderAsync(int buyerId, int orderId, string paypalOrderId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(paypalOrderId))
            throw new ValidationException("paypalOrderId is required", "PAYPAL_ORDER_ID_REQUIRED");

        var order = await _db.OrderTable
            .Include(x => x.buyer)
            .Include(x => x.address)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.product)
                    .ThenInclude(x => x!.seller)
            .Include(x => x.Payment)
            .Include(x => x.ShippingInfo)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        ValidateOrderBeforeCapture(order, buyerId);

        var payment = GetLatestPayment(order!);
        ValidatePayPalPayment(payment, allowCaptured: true);

        if (string.Equals(payment.status, PaymentStatuses.Captured, StringComparison.OrdinalIgnoreCase))
            return OrderMapper.ToDetailDto(order!);

        var accessToken = await GetAccessTokenAsync(ct);

        using var request = BuildCaptureOrderRequest(paypalOrderId, accessToken);
        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new ValidationException("Failed to capture PayPal order", "PAYPAL_CAPTURE_FAILED");

        EnsureCaptureSucceeded(json);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        payment.status = PaymentStatuses.Captured;
        payment.paidAt = DateTime.UtcNow;
        order!.status = OrderStatuses.Paid;

        await CreateSellerSettlementsAsync(order, ct);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Send confirmation email (fire and forget or await, depending on policy)
        try
        {
            await _emailService.SendPaymentSuccessEmailAsync(order, ct);
        }
        catch (Exception ex)
        {
            // Log but don't fail transaction if email fails
            // Consider using a background worker for more robustness
        }

        return OrderMapper.ToDetailDto(order);
    }

    // ── Validation helpers ──────────────────────────────────────────

    private static void ValidateOrderBeforeCreate(OrderTable? order, int buyerId)
    {
        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to pay this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be paid", "ORDER_ALREADY_CANCELLED");

        if (order.addressId == null)
            throw new ValidationException("Order must have a shipping address before payment", "ORDER_ADDRESS_REQUIRED");

        if (!string.Equals(order.status, OrderStatuses.PendingPayment, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Only pending payment order can create PayPal payment", "ORDER_INVALID_STATUS");
    }

    private static void ValidateOrderBeforeCapture(OrderTable? order, int buyerId)
    {
        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to pay this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be paid", "ORDER_ALREADY_CANCELLED");
    }

    private static Payment GetLatestPayment(OrderTable order)
    {
        var payment = order.Payment.OrderByDescending(x => x.id).FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        return payment;
    }

    private static void ValidatePayPalPayment(Payment payment, bool allowCaptured)
    {
        if (!string.Equals(payment.method, PaymentMethods.PayPal, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("This order does not use PayPal", "PAYMENT_METHOD_NOT_PAYPAL");

        if (string.Equals(payment.status, PaymentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled payment cannot be processed", "PAYMENT_ALREADY_CANCELLED");

        if (!allowCaptured &&
            string.Equals(payment.status, PaymentStatuses.Captured, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Order has already been paid", "ORDER_ALREADY_PAID");
        }
    }

    // ── PayPal HTTP helpers ─────────────────────────────────────────

    private HttpRequestMessage BuildCreateOrderRequest(OrderTable order, string accessToken)
    {
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
                        value = (order.totalPrice ?? 0m).ToString("0.00", CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                shipping_preference = "NO_SHIPPING",
                user_action = "PAY_NOW"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/v2/checkout/orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private HttpRequestMessage BuildCaptureOrderRequest(string paypalOrderId, string accessToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        return request;
    }

    private static CreatePayPalPaymentDto ParseCreateOrderResponse(string json)
    {
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
                if (!string.Equals(rel, "approve", StringComparison.OrdinalIgnoreCase))
                    continue;

                approveUrl = link.TryGetProperty("href", out var hrefEl)
                    ? hrefEl.GetString()
                    : null;

                break;
            }
        }

        return new CreatePayPalPaymentDto(paypalOrderId, status, approveUrl);
    }

    private static void EnsureCaptureSucceeded(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = root.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString()
            : null;

        if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new ValidationException("PayPal payment was not completed", "PAYPAL_PAYMENT_NOT_COMPLETED");
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

    // ── Settlement creation ─────────────────────────────────────────

    private async Task CreateSellerSettlementsAsync(OrderTable order, CancellationToken ct)
    {
        foreach (var item in order.OrderItem)
        {
            if (item.product?.sellerId == null)
                continue;

            var exists = await _db.SellerSettlement
                .AsNoTracking()
                .AnyAsync(x => x.orderItemId == item.id, ct);

            if (exists)
                continue;

            var sellerId = item.product.sellerId.Value;
            var grossAmount = (item.unitPrice ?? 0m) * (item.quantity ?? 0);
            var platformFee = Math.Round(grossAmount * 0.05m, 2, MidpointRounding.AwayFromZero);
            var netAmount = grossAmount - platformFee;

            await SellerEntityFactory.GetOrCreateWalletAsync(_db, sellerId, ct);
            await SellerEntityFactory.GetOrCreateTrustProfileAsync(_db, sellerId, ct);

            _db.SellerSettlement.Add(new SellerSettlement
            {
                orderId = order.id,
                orderItemId = item.id,
                sellerId = sellerId,
                grossAmount = grossAmount,
                platformFee = platformFee,
                netAmount = netAmount,
                status = SettlementStatuses.Pending,
                holdReason = null,
                heldAt = null,
                availableAt = null,
                releasedAt = null
            });
        }
    }
}