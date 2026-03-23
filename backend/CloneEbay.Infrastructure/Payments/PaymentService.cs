using System.Globalization;
using System.Net.Http.Headers;
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
    private readonly ISellerHoldPolicyService _holdPolicy;

    private static class OrderStatuses
    {
        public const string PendingPayment = "PENDING_PAYMENT";
        public const string Paid = "PAID";
        public const string Processing = "PROCESSING";
        public const string Cancelled = "CANCELLED";
    }

    private static class PaymentMethods
    {
        public const string PayPal = "PAYPAL";
    }

    private static class PaymentStatuses
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Captured = "CAPTURED";
        public const string Cancelled = "CANCELLED";
    }

    private static class SettlementStatuses
    {
        public const string OnHold = "ON_HOLD";
    }

    private static class ShipmentStatuses
    {
        public const string Pending = "PENDING";
        public const string PickedUp = "PICKED_UP";
        public const string InTransit = "IN_TRANSIT";
        public const string OutForDelivery = "OUT_FOR_DELIVERY";
        public const string Delivered = "DELIVERED";
        public const string Cancelled = "CANCELLED";
    }

    private const int DefaultSimulationMinutes = 180;

    public PaymentService(
        CloneEbayDbContext db,
        HttpClient http,
        IOptions<PayPalOptions> options,
        ISellerHoldPolicyService holdPolicy)
    {
        _db = db;
        _http = http;
        _options = options.Value;
        _holdPolicy = holdPolicy;
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
            return MapOrderDetail(order!);

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

        return MapOrderDetail(order);
    }

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

    private async Task CreateSellerSettlementsAsync(OrderTable order, CancellationToken ct)
    {
        var heldAt = DateTime.UtcNow;

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

            var wallet = await GetOrCreateWalletAsync(sellerId, ct);
            var trustProfile = await GetOrCreateTrustProfileAsync(sellerId, ct);
            var availableAt = _holdPolicy.CalculateAvailableAt(trustProfile, heldAt);

            _db.SellerSettlement.Add(new SellerSettlement
            {
                orderId = order.id,
                orderItemId = item.id,
                sellerId = sellerId,
                grossAmount = grossAmount,
                platformFee = platformFee,
                netAmount = netAmount,
                status = SettlementStatuses.OnHold,
                holdReason = $"LEVEL_{trustProfile.level}",
                heldAt = heldAt,
                availableAt = availableAt,
                releasedAt = null
            });

            wallet.pendingBalance += netAmount;
            wallet.updatedAt = DateTime.UtcNow;
        }
    }

    private async Task<SellerWallet> GetOrCreateWalletAsync(int sellerId, CancellationToken ct)
    {
        var wallet = await _db.SellerWallet.FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);
        if (wallet != null)
            return wallet;

        wallet = new SellerWallet
        {
            sellerId = sellerId,
            pendingBalance = 0m,
            availableBalance = 0m,
            totalEarned = 0m,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _db.SellerWallet.Add(wallet);
        return wallet;
    }

    private async Task<SellerTrustProfile> GetOrCreateTrustProfileAsync(int sellerId, CancellationToken ct)
    {
        var trustProfile = await _db.SellerTrustProfile.FirstOrDefaultAsync(x => x.sellerId == sellerId, ct);
        if (trustProfile != null)
            return trustProfile;

        trustProfile = new SellerTrustProfile
        {
            sellerId = sellerId,
            level = 1,
            completedOrders = 0,
            successfulDeliveries = 0,
            refundCount = 0,
            disputeCount = 0,
            isVerified = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _db.SellerTrustProfile.Add(trustProfile);
        return trustProfile;
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
    orderCode: order.orderCode,
    buyerId: order.buyerId,
    buyerName: order.buyer?.username,
    orderDate: order.orderDate,
    itemSubtotal: order.itemSubtotal ?? 0m,
    shippingTotal: order.shippingTotal ?? 0m,
    discountTotal: order.discountTotal ?? 0m,
    taxTotal: order.taxTotal ?? 0m,
    grandTotal: order.grandTotal ?? (order.totalPrice ?? 0m),
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
                order.address.latitude,
                order.address.longitude,
                order.address.isDefault
            ),
            canUpdateAddress: false,
            addressChangeCount: 0,
            remainingAddressChanges: 0,
            items: order.OrderItem.Select(x => new OrderItemSummaryDto(
                x.id,
                x.productId,
                x.product?.title ?? "Unknown product",
                null,
                x.quantity ?? 0,
                x.unitPrice ?? 0m,
                (x.unitPrice ?? 0m) * (x.quantity ?? 0),
                x.product?.sellerId,
                x.product?.seller?.username
            )).ToList(),
            payments: order.Payment.Select(x => new PaymentSummaryDto(
                x.id,
                x.amount ?? 0m,
                x.method,
                x.status,
                x.paidAt
            )).ToList(),
            shipments: order.Shipment.Select(x => new ShipmentSummaryDto(
                x.id,
                x.sellerId,
                x.shippingMethod,
                x.carrier,
                x.trackingNumber,
                x.status,
                x.shippingCost ?? 0m,
                x.currency,
                x.estimatedShipDate,
                x.estimatedDeliveryDate,
                x.shippedAt,
                x.deliveredAt
            )).ToList()
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
    private static void InitializeShipmentSimulation(OrderTable order, DateTime paidAtUtc)
    {
        if (order.Shipment == null || order.Shipment.Count == 0)
            return;

        foreach (var shipment in order.Shipment)
        {
            if (string.Equals(shipment.status, ShipmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(shipment.status, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                continue;

            shipment.shippedAt = paidAtUtc;
            shipment.estimatedShipDate = paidAtUtc;
            shipment.estimatedDeliveryDate = paidAtUtc.AddMinutes(DefaultSimulationMinutes);

            if (string.IsNullOrWhiteSpace(shipment.trackingNumber))
            {
                shipment.trackingNumber = $"CEB-{paidAtUtc:yyyyMMdd}-{shipment.id:D6}";
            }

            var hasPickedUpEvent = shipment.TrackingEvent.Any(x =>
                string.Equals(x.statusCode, ShipmentStatuses.PickedUp, StringComparison.OrdinalIgnoreCase));

            if (!hasPickedUpEvent)
            {
                shipment.status = ShipmentStatuses.PickedUp;

                shipment.TrackingEvent.Add(new TrackingEvent
                {
                    shipmentId = shipment.id,
                    statusCode = ShipmentStatuses.PickedUp,
                    description = "Carrier picked up the package",
                    location = "Kho xuất phát",
                    latitude = null,
                    longitude = null,
                    eventTime = paidAtUtc
                });
            }
        }
    }
}