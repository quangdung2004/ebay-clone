using System.Text.Json;
using CloneEbay.Application.Common;
using CloneEbay.Application.Payments;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Shipping;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Common.Helpers;
using CloneEbay.Infrastructure.Common.Mappers;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Shipping;

public sealed class ShippingService : IShippingService
{
    private readonly CloneEbayDbContext _db;
    private readonly ISellerHoldPolicyService _holdPolicy;
    private readonly ISeventeenTrackClient _seventeenTrackClient;
    private readonly Application.Common.Interfaces.IGeocodingService _geocodingService;
    private readonly CloneEbay.Application.Orders.IOrderEmailService _emailService;

    public ShippingService(
        CloneEbayDbContext db,
        ISellerHoldPolicyService holdPolicy,
        ISeventeenTrackClient seventeenTrackClient,
        Application.Common.Interfaces.IGeocodingService geocodingService,
        CloneEbay.Application.Orders.IOrderEmailService emailService)
    {
        _db = db;
        _holdPolicy = holdPolicy;
        _seventeenTrackClient = seventeenTrackClient;
        _geocodingService = geocodingService;
        _emailService = emailService;
    }

    public async Task<OrderDetailDto> MarkProcessingAsync(int sellerId, int orderId, CancellationToken ct)
    {
        var order = await LoadOrderForSellerAsync(orderId, sellerId, ct);

        if (!string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Only paid order can move to processing", "ORDER_INVALID_STATUS");

        order.status = OrderStatuses.Processing;
        
        await _db.SaveChangesAsync(ct);

        return OrderMapper.ToDetailDto(order);
    }

    public async Task<OrderDetailDto> CreateShipmentAsync(int sellerId, int orderId, CreateShipmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.carrier))
            throw new ValidationException("Carrier is required", "SHIPMENT_CARRIER_REQUIRED");

        if (string.IsNullOrWhiteSpace(req.trackingNumber))
            throw new ValidationException("Tracking number is required", "SHIPMENT_TRACKING_REQUIRED");

        if (!req.estimatedArrival.HasValue)
            throw new ValidationException("Estimated arrival is required", "SHIPMENT_ESTIMATED_ARRIVAL_REQUIRED");

        var now = DateTime.UtcNow;
        var estimatedArrivalUtc = req.estimatedArrival.Value.Kind == DateTimeKind.Utc
            ? req.estimatedArrival.Value
            : req.estimatedArrival.Value.ToUniversalTime();

        if (estimatedArrivalUtc < now)
            throw new ValidationException(
                "Estimated arrival cannot be in the past",
                "SHIPMENT_ESTIMATED_ARRIVAL_IN_PAST");

        if (estimatedArrivalUtc > now.AddDays(3))
            throw new ValidationException(
                "Estimated arrival cannot be more than 3 days from now",
                "SHIPMENT_ESTIMATED_ARRIVAL_EXCEEDS_3_DAYS");

        var order = await LoadOrderForSellerAsync(orderId, sellerId, ct);

        if (!string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.status, OrderStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only paid or processing order can be shipped", "ORDER_INVALID_STATUS");
        }

        var shipping = await _db.ShippingInfo
            .FirstOrDefaultAsync(x => x.orderId == orderId, ct);

        if (shipping == null)
        {
            shipping = CreateNewShipment(orderId, req, now);
            _db.ShippingInfo.Add(shipping);
        }
        else
        {
            UpdateExistingShipment(shipping, req, now);
        }

        var oldStatus = order.status;
        order.status = OrderStatuses.Shipped;
        await _db.SaveChangesAsync(ct);

        try { await _emailService.SendOrderStatusChangedEmailAsync(order, oldStatus!, order.status, ct); } catch { }

        await _seventeenTrackClient.RegisterTrackingAsync(
            new Register17TrackRequest(
                number: req.trackingNumber.Trim(),
                carrier: null,
                tag: orderId.ToString()),
            ct);

        return await GetOrderDetailForSellerAsync(orderId, sellerId, ct);
    }

    public async Task ApplyTrackingUpdateAsync(
        string trackingNumber,
        string? tag,
        string? mainStatus,
        string? subStatus,
        string? description,
        string? location,
        DateTime? eventTime,
        string rawPayload,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return;

        var shipping = await ResolveShippingByTrackingAsync(trackingNumber, tag, ct);

        if (shipping == null || shipping.orderId == null)
            return;

        var order = await _db.OrderTable
            .Include(x => x.buyer)
            .Include(x => x.address)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.product)
            .Include(x => x.SellerSettlement)
            .FirstOrDefaultAsync(x => x.id == shipping.orderId.Value, ct);

        if (order == null)
            return;

        var normalizedShippingStatus = Map17TrackToShippingStatus(mainStatus, subStatus);

        shipping.status = normalizedShippingStatus;
        shipping.lastSyncedAt = DateTime.UtcNow;
        shipping.lastCheckpoint = description ?? mainStatus ?? subStatus;
        shipping.lastCheckpointTime = eventTime;
        shipping.rawLastPayload = rawPayload;

        var oldStatus = order.status;
        ApplyOrderStatusFromShipping(order, normalizedShippingStatus);

        if (normalizedShippingStatus == ShipmentStatuses.Delivered)
        {
            await ApplyDeliveredAsync(order, shipping, description, eventTime ?? DateTime.UtcNow, ct);
        }

        // Send notification if status changed and order has a buyer with email
        if (order.status != oldStatus && order.buyer != null)
        {
            try { await _emailService.SendOrderStatusChangedEmailAsync(order, oldStatus!, order.status!, ct); } catch {}
        }

        await RecordTrackingEventAsync(shipping.id, "17TRACK", trackingNumber, mainStatus, subStatus,
            description, location, eventTime, rawPayload, ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<OrderDetailDto> ApplyMockStatusAsync(int sellerId, int orderId, MockShipmentStatusRequest req, CancellationToken ct)
    {
        var order = await LoadOrderForSellerAsync(orderId, sellerId, ct);

        var shipping = await _db.ShippingInfo.FirstOrDefaultAsync(x => x.orderId == orderId, ct);
        if (shipping == null)
            throw new ValidationException("Shipment not found", "SHIPMENT_NOT_FOUND");

        var now = req.eventTime ?? DateTime.UtcNow;
        var normalized = NormalizeMockShippingStatus(req.status);

        shipping.status = normalized;
        shipping.lastCheckpoint = req.description ?? normalized;
        shipping.lastCheckpointTime = now;
        shipping.lastSyncedAt = DateTime.UtcNow;

        var oldStatus = order.status;
        ApplyOrderStatusFromShipping(order, normalized);

        if (normalized == ShipmentStatuses.Delivered)
        {
            await ApplyDeliveredAsync(order, shipping, req.description, now, ct);
        }

        if (order.status != oldStatus && order.buyer != null)
        {
            try { await _emailService.SendOrderStatusChangedEmailAsync(order, oldStatus!, order.status!, ct); } catch {}
        }

        await RecordTrackingEventAsync(shipping.id, "MOCK", shipping.trackingNumber ?? "",
            normalized, null, req.description ?? normalized, req.location,
            now, JsonSerializer.Serialize(req), ct);

        await _db.SaveChangesAsync(ct);
        return await GetOrderDetailForSellerAsync(orderId, sellerId, ct);
    }

    // ── Delivery processing ─────────────────────────────────────────

    private async Task ApplyDeliveredAsync(OrderTable order, ShippingInfo shipping, string? description, DateTime deliveredAt, CancellationToken ct)
    {
        if (!string.Equals(order.status, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            order.status = OrderStatuses.Delivered;

        shipping.status = ShipmentStatuses.Delivered;
        shipping.deliveredAt = deliveredAt;
        shipping.lastCheckpoint = description ?? "Delivered";
        shipping.lastCheckpointTime = deliveredAt;

        var sellerIds = order.OrderItem
            .Where(x => x.product?.sellerId != null)
            .Select(x => x.product!.sellerId!.Value)
            .Distinct()
            .ToList();

        var trustProfiles = await _db.SellerTrustProfile
            .Where(x => sellerIds.Contains(x.sellerId))
            .ToDictionaryAsync(x => x.sellerId, ct);

        var wallets = await _db.SellerWallet
            .Where(x => sellerIds.Contains(x.sellerId))
            .ToDictionaryAsync(x => x.sellerId, ct);

        foreach (var settlement in order.SellerSettlement.Where(x => x.status == SettlementStatuses.Pending))
        {
            if (!wallets.TryGetValue(settlement.sellerId, out var wallet))
            {
                wallet = SellerEntityFactory.CreateDefaultWallet(settlement.sellerId);
                _db.SellerWallet.Add(wallet);
                wallets[settlement.sellerId] = wallet;
            }

            if (!trustProfiles.TryGetValue(settlement.sellerId, out var trustProfile))
            {
                trustProfile = SellerEntityFactory.CreateDefaultTrustProfile(settlement.sellerId);
                _db.SellerTrustProfile.Add(trustProfile);
                trustProfiles[settlement.sellerId] = trustProfile;
            }

            settlement.status = SettlementStatuses.OnHold;
            settlement.heldAt = deliveredAt;
            settlement.availableAt = _holdPolicy.CalculateAvailableAt(trustProfile, deliveredAt);
            settlement.holdReason = $"LEVEL_{trustProfile.level}";
            settlement.releasedAt = null;

            wallet.pendingBalance += settlement.netAmount;
            wallet.updatedAt = DateTime.UtcNow;

            trustProfile.successfulDeliveries += 1;
            trustProfile.updatedAt = DateTime.UtcNow;
        }
    }

    // ── Shipping status mapping ─────────────────────────────────────

    private static void ApplyOrderStatusFromShipping(OrderTable order, string shippingStatus)
    {
        if (shippingStatus is ShipmentStatuses.InTransit or ShipmentStatuses.OutForDelivery)
        {
            if (string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(order.status, OrderStatuses.Processing, StringComparison.OrdinalIgnoreCase))
            {
                order.status = OrderStatuses.Shipped;
            }
        }
    }

    private static string Map17TrackToShippingStatus(string? mainStatus, string? subStatus)
    {
        var normalizedMain = (mainStatus ?? "").Trim().ToUpperInvariant();
        var normalizedSub = (subStatus ?? "").Trim().ToUpperInvariant();

        if (normalizedMain.Contains("DELIVERED"))
            return ShipmentStatuses.Delivered;

        if (normalizedMain.Contains("OUTFORDELIVERY") || normalizedMain.Contains("OUT_FOR_DELIVERY"))
            return ShipmentStatuses.OutForDelivery;

        if (normalizedMain.Contains("INTRANSIT") || normalizedMain.Contains("IN_TRANSIT") || normalizedMain.Contains("INFORECEIVED"))
            return ShipmentStatuses.InTransit;

        if (normalizedMain.Contains("EXCEPTION") || normalizedMain.Contains("DELIVERYFAILURE") || normalizedMain.Contains("UNDELIVERED") || normalizedMain.Contains("ALERT"))
            return ShipmentStatuses.Exception;

        if (normalizedSub.Contains("DELIVERED"))
            return ShipmentStatuses.Delivered;

        if (normalizedSub.Contains("OUTFORDELIVERY") || normalizedSub.Contains("OUT_FOR_DELIVERY"))
            return ShipmentStatuses.OutForDelivery;

        return ShipmentStatuses.InTransit;
    }

    private static string NormalizeMockShippingStatus(string? status)
    {
        var normalized = (status ?? "").Trim().ToUpperInvariant();

        return normalized switch
        {
            "IN_TRANSIT" => ShipmentStatuses.InTransit,
            "OUT_FOR_DELIVERY" => ShipmentStatuses.OutForDelivery,
            "DELIVERED" => ShipmentStatuses.Delivered,
            "EXCEPTION" => ShipmentStatuses.Exception,
            _ => throw new ValidationException("Mock status invalid", "MOCK_STATUS_INVALID")
        };
    }

    // ── Shipment factory helpers ────────────────────────────────────

    private static ShippingInfo CreateNewShipment(int orderId, CreateShipmentRequest req, DateTime now)
    {
        return new ShippingInfo
        {
            orderId = orderId,
            carrier = req.carrier.Trim(),
            trackingNumber = req.trackingNumber.Trim(),
            status = ShipmentStatuses.InTransit,
            estimatedArrival = req.estimatedArrival,
            shippedAt = now,
            deliveredAt = null,
            provider = string.IsNullOrWhiteSpace(req.provider) ? "17TRACK" : req.provider.Trim(),
            providerTrackingId = null,
            lastSyncedAt = now,
            lastCheckpoint = "Shipment registered",
            lastCheckpointTime = now,
            rawLastPayload = null
        };
    }

    private static void UpdateExistingShipment(ShippingInfo shipping, CreateShipmentRequest req, DateTime now)
    {
        shipping.carrier = req.carrier.Trim();
        shipping.trackingNumber = req.trackingNumber.Trim();
        shipping.status = ShipmentStatuses.InTransit;
        shipping.estimatedArrival = req.estimatedArrival;
        shipping.shippedAt ??= now;
        shipping.provider = string.IsNullOrWhiteSpace(req.provider) ? "17TRACK" : req.provider.Trim();
        shipping.lastSyncedAt = now;
        shipping.lastCheckpoint = "Shipment updated";
        shipping.lastCheckpointTime = now;
    }

    // ── Tracking event recording ────────────────────────────────────

    private async Task RecordTrackingEventAsync(
        int shippingInfoId, string provider, string trackingNumber,
        string? mainStatus, string? subStatus, string? description,
        string? location, DateTime? eventTime, string rawPayload, CancellationToken ct)
    {
        decimal? lat = null;
        decimal? lon = null;
        string? geoStatus = null;

        if (!string.IsNullOrWhiteSpace(location))
        {
            try 
            {
                var coords = await _geocodingService.GeocodeLocationAsync(location, ct);
                if (coords.HasValue)
                {
                    lat = coords.Value.latitude;
                    lon = coords.Value.longitude;
                    geoStatus = "SUCCESS";
                }
                else
                {
                    geoStatus = "NOT_FOUND";
                }
            }
            catch
            {
                geoStatus = "ERROR";
            }
        }

        _db.ShippingTrackingEvent.Add(new ShippingTrackingEvent
        {
            shippingInfoId = shippingInfoId,
            provider = provider,
            trackingNumber = trackingNumber,
            mainStatus = mainStatus,
            subStatus = subStatus,
            description = description,
            location = location,
            eventTime = eventTime,
            rawPayload = rawPayload,
            createdAt = DateTime.UtcNow,
            latitude = lat,
            longitude = lon,
            normalizedLocation = location,
            geocodeStatus = geoStatus
        });
    }

    // ── Common query helpers ────────────────────────────────────────

    private async Task<ShippingInfo?> ResolveShippingByTrackingAsync(string trackingNumber, string? tag, CancellationToken ct)
    {
        ShippingInfo? shipping = null;

        if (!string.IsNullOrWhiteSpace(tag) && int.TryParse(tag, out var orderIdFromTag))
        {
            shipping = await _db.ShippingInfo
                .Include(x => x.order)
                .FirstOrDefaultAsync(x => x.orderId == orderIdFromTag, ct);
        }

        shipping ??= await _db.ShippingInfo
            .Include(x => x.order)
            .FirstOrDefaultAsync(x => x.trackingNumber == trackingNumber, ct);

        return shipping;
    }

    private async Task<OrderTable> LoadOrderForSellerAsync(int orderId, int sellerId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.buyer)
            .Include(x => x.address)
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.product)
            .Include(x => x.SellerSettlement)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (!order.OrderItem.Any(x => x.product?.sellerId == sellerId))
            throw new ForbiddenException("You are not allowed to update this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be shipped", "ORDER_CANCELLED");

        return order;
    }

    public async Task<OrderDetailDto> GetOrderDetailForSellerAsync(int orderId, int sellerId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .AsNoTracking()
            .Include(x => x.buyer)
            .Include(x => x.address)
            .Include(x => x.OrderItem).ThenInclude(x => x.product).ThenInclude(x => x!.seller)
            .Include(x => x.Payment)
            .Include(x => x.ShippingInfo)
                .ThenInclude(s => s.ShippingTrackingEvent)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (!order.OrderItem.Any(x => x.product?.sellerId == sellerId))
            throw new ForbiddenException("You are not allowed to access this order", "ORDER_FORBIDDEN");

        return OrderMapper.ToDetailDto(order);
    }
}