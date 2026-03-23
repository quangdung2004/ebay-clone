using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Products;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Exceptions;
using CloneEbay.Infrastructure.Common.Helpers;
using CloneEbay.Infrastructure.Common.Mappers;
using CloneEbay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly CloneEbayDbContext _db;

    public OrderService(CloneEbayDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> CreateAsync(int buyerId, CreateOrderRequest req, CancellationToken ct)
    {
        if (req.items == null || req.items.Count == 0)
            throw new ValidationException("Order must contain at least one item", "ORDER_ITEMS_REQUIRED");

        var address = await ResolveBuyerAddressAsync(buyerId, req.addressId, ct);
        var products = await LoadProductsAsync(req, ct);

        var paymentMethod = NormalizePaymentMethod(req.paymentMethod);
        var now = DateTime.UtcNow;

        decimal subtotal = 0m;
        var reservedLines = new List<(Product product, int quantity, decimal unitPrice)>();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var itemReq in req.items)
        {
            var product = products[itemReq.productId];

            ValidateProductForCheckout(product, buyerId);

            await ReserveInventoryAsync(product, itemReq.quantity, ct);

            var remainingQuantity = await GetRemainingQuantityAsync(product.id, ct);
            product.status = remainingQuantity > 0
                ? ProductStatuses.Active
                : ProductStatuses.OutOfStock;

            var unitPrice = product.price ?? 0m;
            subtotal += unitPrice * itemReq.quantity;

            reservedLines.Add((product, itemReq.quantity, unitPrice));
        }

        var shippingFee = ShippingFeeCalculator.Calculate(address, reservedLines);
        var grandTotal = subtotal + shippingFee;

        var order = new OrderTable
        {
            buyerId = buyerId,
            addressId = address.id,
            orderDate = now,
            status = paymentMethod == PaymentMethods.Cod
                ? OrderStatuses.Paid
                : OrderStatuses.PendingPayment,
            subtotalAmount = subtotal,
            shippingFee = shippingFee,
            totalPrice = grandTotal,
            addressChangeCount = 0,
            lastAddressChangedAt = null
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

        _db.Payment.Add(new Payment
        {
            orderId = order.id,
            userId = buyerId,
            amount = grandTotal,
            method = paymentMethod,
            status = paymentMethod == PaymentMethods.Cod
        ? PaymentStatuses.Captured
        : PaymentStatuses.Pending,
            paidAt = paymentMethod == PaymentMethods.Cod
        ? now
        : null
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, order.id, ct);
    }

    public async Task<PagedResponse<OrderSummaryDto>> GetMyOrdersAsync(int buyerId, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

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

        var items = orders.Select(OrderMapper.ToSummaryDto).ToList();
        return new PagedResponse<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<PagedResponse<OrderSummaryDto>> GetSellerOrdersAsync(int sellerId, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _db.OrderTable
            .AsNoTracking()
            .Include(x => x.buyer)
            .Include(x => x.OrderItem).ThenInclude(x => x.product).ThenInclude(x => x!.seller)
            .Include(x => x.Payment)
            .Where(x => x.OrderItem.Any(i => i.product != null && i.product.sellerId == sellerId))
            .OrderByDescending(x => x.orderDate)
            .ThenByDescending(x => x.id);

        var total = await query.CountAsync(ct);

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = orders.Select(OrderMapper.ToSummaryDto).ToList();
        return new PagedResponse<OrderSummaryDto>(items, page, pageSize, total);
    }

    public async Task<OrderDetailDto> GetByIdAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await LoadOrderForBuyerAsync(orderId, buyerId, ct);
        return OrderMapper.ToDetailDto(order);
    }

    public async Task<OrderDetailDto> UpdateAddressAsync(int buyerId, int orderId, int addressId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.OrderTable
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to update this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be updated", "ORDER_ALREADY_CANCELLED");

        if (string.Equals(order.status, OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(order.status, OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(order.status, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Shipping address can only be changed once before shipment is created",
                "ORDER_ADDRESS_CHANGE_NOT_ALLOWED");
        }

        if (order.addressChangeCount >= 1)
            throw new ValidationException(
                "Shipping address can only be changed once",
                "ORDER_ADDRESS_CHANGE_LIMIT_REACHED");

        var address = await _db.Address
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == addressId && x.userId == buyerId, ct);

        if (address == null)
            throw new NotFoundException("Address not found", "ADDRESS_NOT_FOUND");

        if (order.addressId == addressId)
            return await GetByIdAsync(buyerId, orderId, ct);

        var oldAddressId = order.addressId;

        order.addressId = addressId;
        order.addressChangeCount += 1;
        order.lastAddressChangedAt = DateTime.UtcNow;

        var latestPayment = await _db.Payment
            .Where(x => x.orderId == order.id)
            .OrderByDescending(x => x.id)
            .FirstOrDefaultAsync(ct);

        var isCaptured = latestPayment != null &&
            string.Equals(latestPayment.status, PaymentStatuses.Captured, StringComparison.OrdinalIgnoreCase);

        if (!isCaptured)
        {
            var lines = await LoadOrderPricingLinesAsync(order.id, ct);

            var newShippingFee = ShippingFeeCalculator.Calculate(address, lines);
            var subtotal = order.subtotalAmount ?? lines.Sum(x => x.unitPrice * x.quantity);

            order.subtotalAmount = subtotal;
            order.shippingFee = newShippingFee;
            order.totalPrice = subtotal + newShippingFee;

            if (latestPayment != null)
                latestPayment.amount = order.totalPrice;
        }

        if (oldAddressId.HasValue)
        {
            _db.OrderAddressChangeHistory.Add(new OrderAddressChangeHistory
            {
                orderId = order.id,
                oldAddressId = oldAddressId.Value,
                newAddressId = addressId,
                changedByUserId = buyerId,
                reason = "Buyer changed shipping address before shipment",
                changedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, orderId, ct);
    }

    public async Task<OrderDetailDto> CancelAsync(int buyerId, int orderId, CancellationToken ct)
    {
        var order = await _db.OrderTable
            .Include(x => x.OrderItem)
                .ThenInclude(x => x.product)
                    .ThenInclude(x => x!.Inventory)
            .Include(x => x.Payment)
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to cancel this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return await GetByIdAsync(buyerId, orderId, ct);

        if (string.Equals(order.status, OrderStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Paid order cannot be cancelled automatically", "ORDER_CANNOT_CANCEL");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        order.status = OrderStatuses.Cancelled;
        RestoreInventoryOnCancel(order, _db);

        foreach (var payment in order.Payment)
        {
            payment.status = PaymentStatuses.Cancelled;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, orderId, ct);
    }

    // ── Private helpers ─────────────────────────────────────────────

    private async Task<OrderTable> LoadOrderForBuyerAsync(int orderId, int buyerId, CancellationToken ct)
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

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to access this order", "ORDER_FORBIDDEN");

        return order;
    }

    private async Task<Address> ResolveBuyerAddressAsync(int buyerId, int? addressId, CancellationToken ct)
    {
        Address? address;

        if (addressId.HasValue)
        {
            address = await _db.Address
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.id == addressId.Value && x.userId == buyerId, ct);
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

        return address;
    }

    private async Task<Dictionary<int, Product>> LoadProductsAsync(CreateOrderRequest req, CancellationToken ct)
    {
        var productIds = req.items!.Select(x => x.productId).Distinct().ToList();

        if (productIds.Count != req.items.Count)
            throw new ValidationException("Duplicate product in order is not allowed", "DUPLICATE_ORDER_ITEM");

        var products = await _db.Product
            .Where(x => productIds.Contains(x.id) && x.isDeleted != true)
            .ToDictionaryAsync(x => x.id, ct);

        if (products.Count != productIds.Count)
            throw new NotFoundException("One or more products were not found", "PRODUCT_NOT_FOUND");

        return products;
    }

    private static void ValidateProductForCheckout(Product product, int buyerId)
    {
        if (product.sellerId == buyerId)
            throw new ValidationException($"You cannot buy your own product: {product.title}", "ORDER_SELF_BUY_NOT_ALLOWED");

        if (product.isAuction == true)
            throw new ValidationException($"Auction product cannot be purchased via checkout: {product.title}", "AUCTION_CHECKOUT_NOT_ALLOWED");

        if (!string.Equals(product.status, ProductStatuses.Active, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException($"Product is not available for checkout: {product.title}", "PRODUCT_NOT_AVAILABLE");
    }

    private async Task ReserveInventoryAsync(Product product, int quantity, CancellationToken ct)
    {
        var reserved = await _db.Inventory
            .Where(x => x.productId == product.id && (x.quantity ?? 0) >= quantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.quantity, x => (int?)((x.quantity ?? 0) - quantity))
                .SetProperty(x => x.lastUpdated, _ => DateTime.UtcNow), ct);

        if (reserved == 0)
            throw new ValidationException($"Insufficient stock for product: {product.title}", "INSUFFICIENT_STOCK");
    }

    private async Task<int> GetRemainingQuantityAsync(int productId, CancellationToken ct)
    {
        return await _db.Inventory
            .AsNoTracking()
            .Where(x => x.productId == productId)
            .Select(x => x.quantity ?? 0)
            .FirstOrDefaultAsync(ct);
    }

    private static string NormalizePaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ValidationException("Payment method is required", "PAYMENT_METHOD_REQUIRED");

        var normalized = method.Trim().ToUpperInvariant();

        if (normalized is not (PaymentMethods.Cod or PaymentMethods.PayPal))
            throw new ValidationException("Payment method must be COD or PAYPAL", "PAYMENT_METHOD_INVALID");

        return normalized;
    }

    private static void RestoreInventoryOnCancel(OrderTable order, CloneEbayDbContext db)
    {
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
                db.Inventory.Add(inventory);
            }

            inventory.quantity = (inventory.quantity ?? 0) + (item.quantity ?? 0);
            inventory.lastUpdated = DateTime.UtcNow;
            item.product.status = ProductStatuses.Active;
        }
    }

    private async Task<List<(Product product, int quantity, decimal unitPrice)>> LoadOrderPricingLinesAsync(int orderId, CancellationToken ct)
    {
        var orderItems = await _db.OrderItem
            .Include(x => x.product)
            .Where(x => x.orderId == orderId)
            .ToListAsync(ct);

        return orderItems
            .Where(x => x.product != null)
            .Select(x => (
                product: x.product!,
                quantity: x.quantity ?? 0,
                unitPrice: x.unitPrice ?? 0m))
            .ToList();
    }
}