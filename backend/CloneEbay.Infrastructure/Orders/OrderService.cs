using CloneEbay.Application.Orders;
using CloneEbay.Application.Shipping;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Persistence;
using CloneEbay.Infrastructure.Products;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly CloneEbayDbContext _db;
    private readonly IShippingService _shipping;

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
    private const string AddressChangedTrackingCode = "ADDRESS_CHANGED";
    private const string AddressChangedLocationPrefix = "ORDER_ADDRESS_CHANGE:";
    private const int PaidAddressChangeLimit = 1;

    public OrderService(CloneEbayDbContext db, IShippingService shipping)
    {
        _db = db;
        _shipping = shipping;
    }

    public async Task<OrderDetailDto> CreateAsync(int buyerId, CreateOrderRequest req, CancellationToken ct)
    {
        if (req.items == null || req.items.Count == 0)
            throw new ValidationException("Order must contain at least one item", "ORDER_ITEMS_REQUIRED");

        Address? address;

        if (req.addressId.HasValue)
        {
            address = await _db.Address
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == req.addressId.Value && x.userId == buyerId, ct);
        }
        else
        {
            address = await _db.Address
                .AsNoTracking()
                .Where(x => x.userId == buyerId)
                .OrderByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);
        }

        if (address == null)
            throw new NotFoundException("Address not found for current user", "ADDRESS_NOT_FOUND");

        var productIds = req.items.Select(x => x.productId).Distinct().ToList();

        if (productIds.Count != req.items.Count)
            throw new ValidationException("Duplicate product in order is not allowed", "DUPLICATE_ORDER_ITEM");

        var products = await _db.Product
            .Where(x => productIds.Contains(x.id) && x.isDeleted != true)
            .ToDictionaryAsync(x => x.id, ct);

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products were not found", "PRODUCT_NOT_FOUND");

        var paymentMethod = NormalizePaymentMethod(req.paymentMethod);
        var now = DateTime.UtcNow;

        decimal itemSubtotal = 0m;
        decimal discountTotal = 0m;
        decimal taxTotal = 0m;

        var reservedLines = new List<(Product product, int quantity, decimal unitPrice)>();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var itemReq in req.items)
        {
            var product = products[itemReq.productId];

            if (product.sellerId == null)
                throw new ValidationException($"Product does not have seller: {product.title}", "PRODUCT_SELLER_REQUIRED");

            if (product.sellerId == buyerId)
                throw new ValidationException($"You cannot buy your own product: {product.title}", "ORDER_SELF_BUY_NOT_ALLOWED");

            if (product.isAuction == true)
                throw new ValidationException($"Auction product cannot be purchased via checkout: {product.title}", "AUCTION_CHECKOUT_NOT_ALLOWED");

            if (!string.Equals(product.status, ProductStatuses.Active, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException($"Product is not available for checkout: {product.title}", "PRODUCT_NOT_AVAILABLE");

            var reserved = await _db.Inventory
                .Where(x => x.productId == product.id && (x.quantity ?? 0) >= itemReq.quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.quantity, x => (int?)((x.quantity ?? 0) - itemReq.quantity))
                    .SetProperty(x => x.lastUpdated, _ => DateTime.UtcNow), ct);

            if (reserved == 0)
                throw new ValidationException($"Insufficient stock for product: {product.title}", "INSUFFICIENT_STOCK");

            var remainingQuantity = await _db.Inventory
                .AsNoTracking()
                .Where(x => x.productId == product.id)
                .Select(x => x.quantity ?? 0)
                .FirstOrDefaultAsync(ct);

            product.status = remainingQuantity > 0
                ? ProductStatuses.Active
                : ProductStatuses.OutOfStock;

            var unitPrice = product.price ?? 0m;
            itemSubtotal += unitPrice * itemReq.quantity;

            reservedLines.Add((product, itemReq.quantity, unitPrice));
        }

        var shippingQuote = await _shipping.QuoteAsync(
            address,
            reservedLines.Select(x => (x.product, x.quantity)).ToList(),
            ct);

        var shippingTotal = shippingQuote.shippingTotal;
        var grandTotal = itemSubtotal + shippingTotal + taxTotal - discountTotal;

        var order = new OrderTable
        {
            buyerId = buyerId,
            addressId = address.id,
            orderDate = now,
            status = paymentMethod == "COD"
                ? OrderStatuses.Confirmed
                : OrderStatuses.PendingPayment,
            itemSubtotal = itemSubtotal,
            shippingTotal = shippingTotal,
            discountTotal = discountTotal,
            taxTotal = taxTotal,
            grandTotal = grandTotal,
            totalPrice = grandTotal
        };

        _db.OrderTable.Add(order);
        await _db.SaveChangesAsync(ct);

        foreach (var line in reservedLines)
        {
            _db.OrderItem.Add(new OrderItem
            {
                orderId = order.id,
                productId = line.product.id,
                quantity = line.quantity,
                unitPrice = line.unitPrice
            });
        }

        await _db.SaveChangesAsync(ct);

        var createdOrderItems = await _db.OrderItem
            .Where(x => x.orderId == order.id)
            .Include(x => x.product)
            .ToListAsync(ct);

        var groupedOrderItems = createdOrderItems
            .Where(x => x.product?.sellerId != null)
            .GroupBy(x => x.product!.sellerId!.Value)
            .ToList();

        foreach (var group in groupedOrderItems)
        {
            var shipmentDraft = shippingQuote.shipments.First(x => x.sellerId == group.Key);

            var shipment = new Shipment
            {
                orderId = order.id,
                sellerId = shipmentDraft.sellerId,
                originAddressId = shipmentDraft.originAddressId,
                destinationAddressId = address.id,
                shippingMethod = shipmentDraft.shippingMethod,
                carrier = shipmentDraft.carrier,
                trackingNumber = null,
                status = ShipmentStatuses.Pending,
                shippingCost = shipmentDraft.shippingCost,
                currency = "VND",
                estimatedShipDate = DateTime.UtcNow.AddDays(1),
                estimatedDeliveryDate = shipmentDraft.estimatedDeliveryDate,
                shippedAt = null,
                deliveredAt = null,
                createdAt = DateTime.UtcNow
            };

            _db.Shipment.Add(shipment);
            await _db.SaveChangesAsync(ct);

            foreach (var orderItem in group)
            {
                _db.ShipmentItem.Add(new ShipmentItem
                {
                    shipmentId = shipment.id,
                    orderItemId = orderItem.id,
                    quantity = orderItem.quantity ?? 0
                });
            }

            _db.TrackingEvent.Add(new TrackingEvent
            {
                shipmentId = shipment.id,
                statusCode = ShipmentStatuses.Pending,
                description = "Shipment created",
                location = null,
                latitude = null,
                longitude = null,
                eventTime = DateTime.UtcNow
            });
        }

        _db.Payment.Add(new Payment
        {
            orderId = order.id,
            userId = buyerId,
            amount = grandTotal,
            method = paymentMethod,
            status = PaymentStatuses.Pending,
            paidAt = null
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, order.id, ct);
    }

    public async Task<OrderTrackingDto> GetTrackingAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
                .ThenInclude(x => x.TrackingEvent)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to access this tracking", "ORDER_FORBIDDEN");

        var changed = SynchronizeShipmentSimulation(order, DateTime.UtcNow);

        if (changed)
        {
            await _db.SaveChangesAsync(ct);

            order = await _db.OrderTable
                .AsNoTracking()
                .Include(x => x.Shipment)
                    .ThenInclude(x => x.TrackingEvent)
                .FirstAsync(x => x.id == orderId, ct);
        }

        var shipments = order.Shipment
            .OrderBy(x => x.id)
            .Select(MapShipmentTracking)
            .ToList();

        var deliveredShipments = shipments.Count(x =>
            string.Equals(x.status, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase));

        return new OrderTrackingDto(
            orderId: order.id,
            orderStatus: order.status,
            overallShipmentStatus: CalculateOverallShipmentStatus(shipments),
            orderDate: order.orderDate,
            totalShipments: shipments.Count,
            deliveredShipments: deliveredShipments,
            shipments: shipments);
    }
    public async Task<PagedResponse<OrderSummaryDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.OrderTable
            .AsNoTracking()
            .Include(x => x.buyer)
            .Include(x => x.OrderItem).ThenInclude(x => x.product).ThenInclude(x => x!.seller)
            .Include(x => x.Payment)
            .Where(x => x.buyerId == buyerId)
            .OrderByDescending(x => x.orderDate)
            .ThenByDescending(x => x.id);

        var total = await query.CountAsync(ct);

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = orders.Select(MapSummary).ToList();
        return new PagedResponse<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<OrderDetailDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
     .AsNoTracking()
     .Include(x => x.buyer)
     .Include(x => x.address)
     .Include(x => x.OrderItem).ThenInclude(x => x.product).ThenInclude(x => x!.seller)
     .Include(x => x.Payment)
     .Include(x => x.Shipment)
         .ThenInclude(x => x.TrackingEvent)
     .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to access this order", "ORDER_FORBIDDEN");

        return MapDetail(order);
    }

    public async Task<OrderDetailDto> UpdateAddressAsync(int buyerId, int orderId, int addressId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
                .ThenInclude(x => x.TrackingEvent)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to update this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be updated", "ORDER_ALREADY_CANCELLED");

        var isPaidOrder = string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase);

   

        // đơn PAID chỉ được đổi 1 lần
        var paidAddressChangeCount = CountPaidAddressChanges(order);
        if (isPaidOrder && paidAddressChangeCount >= PaidAddressChangeLimit)
            throw new ValidationException("Paid order shipping address can only be changed once", "ORDER_ADDRESS_CHANGE_LIMIT_REACHED");

        var address = await _db.Address
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == addressId && x.userId == buyerId, ct);

        if (address == null)
            throw new NotFoundException("Address not found", "ADDRESS_NOT_FOUND");

        if (order.addressId == addressId)
            throw new ValidationException("This address is already selected for the order", "ORDER_ADDRESS_UNCHANGED");

        order.addressId = addressId;

        foreach (var shipment in order.Shipment)
        {
            if (string.Equals(shipment.status, ShipmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                continue;

            shipment.destinationAddressId = addressId;

            // Chỉ log lần đổi địa chỉ nếu order đã PAID
            if (isPaidOrder)
            {
                shipment.TrackingEvent.Add(new TrackingEvent
                {
                    statusCode = AddressChangedTrackingCode,
                    description = $"Buyer updated shipping address to address #{addressId}",
                    location = $"{AddressChangedLocationPrefix}{addressId}",
                    latitude = address.latitude,
                    longitude = address.longitude,
                    eventTime = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(buyerId, orderId, ct);
    }

    public async Task<ShipmentTrackingDto> UpdateShipmentTrackingAsync(int sellerId, int shipmentId, UpdateShipmentTrackingRequest req, CancellationToken ct)
    {
        var shipment = await _db.Shipment
            .Include(x => x.order)
            .Include(x => x.TrackingEvent)
            .FirstOrDefaultAsync(x => x.id == shipmentId, ct);

        if (shipment == null)
            throw new NotFoundException("Shipment not found", "SHIPMENT_NOT_FOUND");

        if (shipment.sellerId != sellerId)
            throw new ForbiddenException("You are not allowed to update this shipment", "SHIPMENT_FORBIDDEN");

        if (shipment.order == null)
            throw new NotFoundException("Order not found for shipment", "ORDER_NOT_FOUND");

        if (string.Equals(shipment.order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be updated", "ORDER_ALREADY_CANCELLED");

        if (IsTerminalShipmentStatus(shipment.status))
            throw new ValidationException("Completed or cancelled shipment cannot be updated", "SHIPMENT_ALREADY_FINALIZED");

        var normalizedStatus = NormalizeShipmentStatus(req.status);
        var eventTime = req.eventTime?.ToUniversalTime() ?? DateTime.UtcNow;

        shipment.status = normalizedStatus;

        if (!string.IsNullOrWhiteSpace(req.trackingNumber))
        {
            shipment.trackingNumber = req.trackingNumber.Trim();
        }
        else if (string.IsNullOrWhiteSpace(shipment.trackingNumber) && HasPhysicalMovement(normalizedStatus))
        {
            shipment.trackingNumber = GenerateTrackingNumber(shipment.id);
        }

        if (HasPhysicalMovement(normalizedStatus) && shipment.shippedAt == null)
            shipment.shippedAt = eventTime;

        if (string.Equals(normalizedStatus, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            shipment.deliveredAt = eventTime;

        var description = string.IsNullOrWhiteSpace(req.description)
            ? BuildDefaultTrackingDescription(normalizedStatus)
            : req.description.Trim();

        var location = string.IsNullOrWhiteSpace(req.location)
            ? null
            : req.location.Trim();

        var (latitude, longitude) = SanitizeTrackingCoordinates(req.latitude, req.longitude);

        _db.TrackingEvent.Add(new TrackingEvent
        {
            shipmentId = shipment.id,
            statusCode = normalizedStatus,
            description = description,
            location = location,
            latitude = latitude,
            longitude = longitude,
            eventTime = eventTime
        });

        await _db.SaveChangesAsync(ct);

        shipment = await _db.Shipment
            .AsNoTracking()
            .Include(x => x.TrackingEvent)
            .FirstAsync(x => x.id == shipmentId, ct);

        return MapShipmentTracking(shipment);
    }

    public async Task<OrderDetailDto> PayAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
                .ThenInclude(x => x.TrackingEvent)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to pay this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be paid", "ORDER_ALREADY_CANCELLED");

        if (order.addressId == null)
            throw new ValidationException("Order must have a shipping address before payment", "ORDER_ADDRESS_REQUIRED");

        var payment = order.Payment
            .OrderByDescending(x => x.id)
            .FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        if (string.Equals(payment.status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return await GetByIdAsync(buyerId, orderId, ct);

        var paidAt = DateTime.UtcNow;

        payment.status = PaymentStatuses.Paid;
        payment.paidAt = paidAt;
        order.status = OrderStatuses.Paid;

        InitializeShipmentSimulation(order, paidAt);

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(buyerId, orderId, ct);
    }

    public async Task<OrderDetailDto> CancelAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.OrderItem)
            .ThenInclude(x => x.product)
            .ThenInclude(x => x!.Inventory)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to cancel this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return await GetByIdAsync(buyerId, order.id, ct);

        if (string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Paid order cannot be cancelled automatically", "ORDER_CANNOT_CANCEL");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        order.status = OrderStatuses.Cancelled;

        foreach (var item in order.OrderItem)
        {
            if (item.product == null)
                continue;

            var inventory = item.product.Inventory.FirstOrDefault();
            if (inventory == null)
            {
                inventory = new Inventory
                {
                    productId = item.product.id,
                    quantity = 0,
                    lastUpdated = DateTime.UtcNow
                };
                _db.Inventory.Add(inventory);
            }

            inventory.quantity = (inventory.quantity ?? 0) + (item.quantity ?? 0);
            inventory.lastUpdated = DateTime.UtcNow;
            item.product.status = ProductStatuses.Active;
        }

        foreach (var shipment in order.Shipment)
        {
            shipment.status = ShipmentStatuses.Cancelled;

            _db.TrackingEvent.Add(new TrackingEvent
            {
                shipmentId = shipment.id,
                statusCode = ShipmentStatuses.Cancelled,
                description = "Shipment cancelled",
                location = null,
                latitude = null,
                longitude = null,
                eventTime = DateTime.UtcNow
            });
        }

        foreach (var payment in order.Payment)
        {
            payment.status = PaymentStatuses.Cancelled;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, orderId, ct);
    }

    private static string NormalizePaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ValidationException("Payment method is required", "PAYMENT_METHOD_REQUIRED");

        return method.Trim().ToUpperInvariant();
    }

    private static string NormalizeShipmentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ValidationException("Shipment status is required", "SHIPMENT_STATUS_REQUIRED");

        var normalized = status.Trim().ToUpperInvariant();

        return normalized switch
        {
            ShipmentStatuses.Pending => normalized,
            ShipmentStatuses.PickedUp => normalized,
            ShipmentStatuses.InTransit => normalized,
            ShipmentStatuses.OutForDelivery => normalized,
            ShipmentStatuses.Delivered => normalized,
            ShipmentStatuses.Cancelled => normalized,
            _ => throw new ValidationException("Unsupported shipment status", "SHIPMENT_STATUS_INVALID")
        };
    }

    private static bool HasPhysicalMovement(string? shipmentStatus)
        => string.Equals(shipmentStatus, ShipmentStatuses.PickedUp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shipmentStatus, ShipmentStatuses.InTransit, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shipmentStatus, ShipmentStatuses.OutForDelivery, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shipmentStatus, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase);

    private static (decimal? latitude, decimal? longitude) SanitizeTrackingCoordinates(decimal? latitude, decimal? longitude)
    {
        if (!latitude.HasValue && !longitude.HasValue)
            return (null, null);

        if (!latitude.HasValue || !longitude.HasValue)
            return (null, null);

        if (latitude.Value == 0m && longitude.Value == 0m)
            return (null, null);

        return (latitude, longitude);
    }

    private static bool IsTerminalShipmentStatus(string? shipmentStatus)
        => string.Equals(shipmentStatus, ShipmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shipmentStatus, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase);

    private static string GenerateTrackingNumber(int shipmentId)
        => $"CEB-{DateTime.UtcNow:yyyyMMdd}-{shipmentId:D6}";

    private static string BuildDefaultTrackingDescription(string status)
    {
        return status switch
        {
            ShipmentStatuses.Pending => "Shipment is waiting for seller confirmation",
            ShipmentStatuses.PickedUp => "Carrier picked up the package",
            ShipmentStatuses.InTransit => "Package is on the way",
            ShipmentStatuses.OutForDelivery => "Package is out for delivery",
            ShipmentStatuses.Delivered => "Package was delivered successfully",
            ShipmentStatuses.Cancelled => "Shipment was cancelled",
            _ => "Shipment status updated"
        };
    }

    private static string? CalculateOverallShipmentStatus(IReadOnlyList<ShipmentTrackingDto> shipments)
    {
        if (shipments.Count == 0)
            return null;

        if (shipments.All(x => string.Equals(x.status, ShipmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)))
            return ShipmentStatuses.Cancelled;

        if (shipments.All(x => string.Equals(x.status, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase)))
            return ShipmentStatuses.Delivered;

        if (shipments.Any(x => string.Equals(x.status, ShipmentStatuses.OutForDelivery, StringComparison.OrdinalIgnoreCase)))
            return ShipmentStatuses.OutForDelivery;

        if (shipments.Any(x => string.Equals(x.status, ShipmentStatuses.InTransit, StringComparison.OrdinalIgnoreCase)))
            return ShipmentStatuses.InTransit;

        if (shipments.Any(x => string.Equals(x.status, ShipmentStatuses.PickedUp, StringComparison.OrdinalIgnoreCase)))
            return ShipmentStatuses.PickedUp;

        return ShipmentStatuses.Pending;
    }

    private static OrderSummaryDto MapSummary(OrderTable order)
    {
        var items = order.OrderItem.Select(MapItem).ToList();
        var payments = order.Payment.Select(MapPayment).ToList();

        return new OrderSummaryDto(
            id: order.id,
            buyerId: order.buyerId,
            buyerName: order.buyer?.username,
            addressId: order.addressId,
            orderDate: order.orderDate,
            itemSubtotal: order.itemSubtotal ?? 0m,
            shippingTotal: order.shippingTotal ?? 0m,
            discountTotal: order.discountTotal ?? 0m,
            taxTotal: order.taxTotal ?? 0m,
            grandTotal: order.grandTotal ?? (order.totalPrice ?? 0m),
            totalPrice: order.totalPrice ?? 0m,
            status: order.status,
            totalItems: order.OrderItem.Sum(x => x.quantity ?? 0),
            items: items,
            payments: payments
        );
    }

    private static OrderDetailDto MapDetail(OrderTable order)
    {
        var addressChangeCount = CountPaidAddressChanges(order);

        var canUpdateAddress = !string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)
            && (!string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase)
                || addressChangeCount < PaidAddressChangeLimit);

        var remainingAddressChanges = string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(0, PaidAddressChangeLimit - addressChangeCount)
            : PaidAddressChangeLimit;

        return new OrderDetailDto(
            id: order.id,
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
            canUpdateAddress: canUpdateAddress,
            addressChangeCount: addressChangeCount,
            remainingAddressChanges: remainingAddressChanges,
            items: order.OrderItem.Select(MapItem).ToList(),
            payments: order.Payment.Select(MapPayment).ToList(),
            shipments: order.Shipment.Select(MapShipment).ToList()
        );
    }
    private static int CountPaidAddressChanges(OrderTable order)
    => order.Shipment
        .SelectMany(x => x.TrackingEvent)
        .Count(x =>
            string.Equals(x.statusCode, AddressChangedTrackingCode, StringComparison.OrdinalIgnoreCase) &&
            (x.location ?? string.Empty).StartsWith(AddressChangedLocationPrefix, StringComparison.OrdinalIgnoreCase));

    private static ShipmentSummaryDto MapShipment(Shipment shipment)
    {
        return new ShipmentSummaryDto(
            id: shipment.id,
            sellerId: shipment.sellerId,
            shippingMethod: shipment.shippingMethod,
            carrier: shipment.carrier,
            trackingNumber: shipment.trackingNumber,
            status: shipment.status,
            shippingCost: shipment.shippingCost ?? 0m,
            currency: shipment.currency,
            estimatedShipDate: shipment.estimatedShipDate,
            estimatedDeliveryDate: shipment.estimatedDeliveryDate,
            shippedAt: shipment.shippedAt,
            deliveredAt: shipment.deliveredAt
        );
    }

    private static ShipmentTrackingDto MapShipmentTracking(Shipment shipment)
    {
        return new ShipmentTrackingDto(
            id: shipment.id,
            orderId: shipment.orderId,
            sellerId: shipment.sellerId,
            shippingMethod: shipment.shippingMethod,
            carrier: shipment.carrier,
            trackingNumber: shipment.trackingNumber,
            status: shipment.status,
            shippingCost: shipment.shippingCost ?? 0m,
            currency: shipment.currency,
            estimatedShipDate: shipment.estimatedShipDate,
            estimatedDeliveryDate: shipment.estimatedDeliveryDate,
            shippedAt: shipment.shippedAt,
            deliveredAt: shipment.deliveredAt,
            events: shipment.TrackingEvent
                .OrderBy(x => x.eventTime ?? DateTime.MinValue)
                .ThenBy(x => x.id)
                .Select(MapTrackingEvent)
                .ToList());
    }

    private static TrackingEventDto MapTrackingEvent(TrackingEvent trackingEvent)
    {
        return new TrackingEventDto(
            id: trackingEvent.id,
            statusCode: trackingEvent.statusCode,
            description: trackingEvent.description,
            location: trackingEvent.location,
            latitude: trackingEvent.latitude,
            longitude: trackingEvent.longitude,
            eventTime: trackingEvent.eventTime
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
                shipment.trackingNumber = GenerateTrackingNumber(shipment.id);
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

    private static bool SynchronizeShipmentSimulation(OrderTable order, DateTime nowUtc)
    {
        var changed = false;

        if (!string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var shipment in order.Shipment)
        {
            if (string.Equals(shipment.status, ShipmentStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
                continue;

            if (string.Equals(shipment.status, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
                continue;

            var startAt = shipment.shippedAt
                ?? order.Payment
                    .Where(x => string.Equals(x.status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.paidAt)
                    .Select(x => x.paidAt)
                    .FirstOrDefault();

            if (!startAt.HasValue)
                continue;

            shipment.shippedAt = startAt.Value;
            shipment.estimatedShipDate = startAt.Value;
            shipment.estimatedDeliveryDate = startAt.Value.AddMinutes(DefaultSimulationMinutes);

            var endAt = shipment.estimatedDeliveryDate ?? startAt.Value.AddMinutes(DefaultSimulationMinutes);
            var totalSeconds = Math.Max((endAt - startAt.Value).TotalSeconds, 1);
            var elapsedSeconds = Math.Max((nowUtc - startAt.Value).TotalSeconds, 0);
            var progress = Math.Min(elapsedSeconds / totalSeconds, 1d);

            var nextStatus = progress switch
            {
                >= 1d => ShipmentStatuses.Delivered,
                >= 0.8d => ShipmentStatuses.OutForDelivery,
                >= 0.2d => ShipmentStatuses.InTransit,
                _ => ShipmentStatuses.PickedUp
            };

            if (!string.Equals(shipment.status, nextStatus, StringComparison.OrdinalIgnoreCase))
            {
                shipment.status = nextStatus;
                changed = true;
            }

            if (string.Equals(nextStatus, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase)
                && shipment.deliveredAt == null)
            {
                shipment.deliveredAt = endAt;
                changed = true;
            }

            var hasEvent = shipment.TrackingEvent.Any(x =>
                string.Equals(x.statusCode, nextStatus, StringComparison.OrdinalIgnoreCase));

            if (!hasEvent)
            {
                shipment.TrackingEvent.Add(new TrackingEvent
                {
                    shipmentId = shipment.id,
                    statusCode = nextStatus,
                    description = BuildDefaultTrackingDescription(nextStatus),
                    location = BuildSimulationLocationLabel(progress),
                    latitude = null,
                    longitude = null,
                    eventTime = string.Equals(nextStatus, ShipmentStatuses.Delivered, StringComparison.OrdinalIgnoreCase)
                        ? endAt
                        : nowUtc
                });

                changed = true;
            }
        }

        return changed;
    }

    private static string BuildSimulationLocationLabel(double progress)
    {
        if (progress >= 1d)
            return "Đã giao thành công";

        if (progress >= 0.8d)
            return "Khu vực gần địa chỉ nhận hàng";

        if (progress >= 0.2d)
            return "Đang trên đường vận chuyển";

        return "Kho xuất phát";
    }

    public async Task<QuoteOrderDto> QuoteAsync(int buyerId, QuoteOrderRequest req, CancellationToken ct)
    {
        if (req.items == null || req.items.Count == 0)
            throw new ValidationException("Order must contain at least one item", "ORDER_ITEMS_REQUIRED");

        Address? address;

        if (req.addressId.HasValue)
        {
            address = await _db.Address
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == req.addressId.Value && x.userId == buyerId, ct);
        }
        else
        {
            address = await _db.Address
                .AsNoTracking()
                .Where(x => x.userId == buyerId)
                .OrderByDescending(x => x.isDefault == true)
                .ThenBy(x => x.id)
                .FirstOrDefaultAsync(ct);
        }

        if (address == null)
            throw new NotFoundException("Address not found for current user", "ADDRESS_NOT_FOUND");

        var productIds = req.items.Select(x => x.productId).Distinct().ToList();

        if (productIds.Count != req.items.Count)
            throw new ValidationException("Duplicate product in order is not allowed", "DUPLICATE_ORDER_ITEM");

        var products = await _db.Product
            .AsNoTracking()
            .Where(x => productIds.Contains(x.id) && x.isDeleted != true)
            .ToDictionaryAsync(x => x.id, ct);

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products were not found", "PRODUCT_NOT_FOUND");

        decimal itemSubtotal = 0m;
        decimal discountTotal = 0m;
        decimal taxTotal = 0m;

        var quoteLines = new List<(Product product, int quantity)>();

        foreach (var itemReq in req.items)
        {
            var product = products[itemReq.productId];

            if (product.sellerId == null)
                throw new ValidationException($"Product does not have seller: {product.title}", "PRODUCT_SELLER_REQUIRED");

            if (product.sellerId == buyerId)
                throw new ValidationException($"You cannot buy your own product: {product.title}", "ORDER_SELF_BUY_NOT_ALLOWED");

            if (product.isAuction == true)
                throw new ValidationException($"Auction product cannot be purchased via checkout: {product.title}", "AUCTION_CHECKOUT_NOT_ALLOWED");

            if (!string.Equals(product.status, ProductStatuses.Active, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException($"Product is not available for checkout: {product.title}", "PRODUCT_NOT_AVAILABLE");

            var availableQuantity = await _db.Inventory
                .AsNoTracking()
                .Where(x => x.productId == product.id)
                .Select(x => x.quantity ?? 0)
                .FirstOrDefaultAsync(ct);

            if (availableQuantity < itemReq.quantity)
                throw new ValidationException($"Insufficient stock for product: {product.title}", "INSUFFICIENT_STOCK");

            var unitPrice = product.price ?? 0m;
            itemSubtotal += unitPrice * itemReq.quantity;

            quoteLines.Add((product, itemReq.quantity));
        }

        var shippingQuote = await _shipping.QuoteAsync(address, quoteLines, ct);
        var shippingTotal = shippingQuote.shippingTotal;
        var grandTotal = itemSubtotal + shippingTotal + taxTotal - discountTotal;

        var shipments = shippingQuote.shipments
            .OrderBy(x => x.sellerId)
            .Select((shipment, index) => new ShipmentSummaryDto(
                id: -(index + 1),
                sellerId: shipment.sellerId,
                shippingMethod: shipment.shippingMethod,
                carrier: shipment.carrier,
                trackingNumber: null,
                status: ShipmentStatuses.Pending,
                shippingCost: shipment.shippingCost,
                currency: "VND",
                estimatedShipDate: DateTime.UtcNow.AddDays(1),
                estimatedDeliveryDate: shipment.estimatedDeliveryDate,
                shippedAt: null,
                deliveredAt: null
            ))
            .ToList();

        return new QuoteOrderDto(
            itemSubtotal: itemSubtotal,
            shippingTotal: shippingTotal,
            discountTotal: discountTotal,
            taxTotal: taxTotal,
            grandTotal: grandTotal,
            shipments: shipments);
    }
}