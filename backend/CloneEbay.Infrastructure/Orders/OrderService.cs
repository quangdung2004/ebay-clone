using CloneEbay.Application.Orders;
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

    public OrderService(CloneEbayDbContext db)
    {
        _db = db;
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
        decimal total = 0m;

        var reservedLines = new List<(Product product, int quantity, decimal unitPrice)>();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var itemReq in req.items)
        {
            var product = products[itemReq.productId];

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
            total += unitPrice * itemReq.quantity;

            reservedLines.Add((product, itemReq.quantity, unitPrice));
        }

        var order = new OrderTable
        {
            buyerId = buyerId,
            addressId = address.id,
            orderDate = now,
            status = paymentMethod == "COD"
                ? OrderStatuses.Confirmed
                : OrderStatuses.PendingPayment,
            totalPrice = total
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
            amount = total,
            method = paymentMethod,
            status = PaymentStatuses.Pending,
            paidAt = null
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(buyerId, order.id, ct);
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
            .Include(x => x.ShippingInfo)
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
            .FirstOrDefaultAsync(x => x.id == orderId, ct);

        if (order == null)
            throw new NotFoundException("Order not found", "ORDER_NOT_FOUND");

        if (order.buyerId != buyerId)
            throw new ForbiddenException("You are not allowed to update this order", "ORDER_FORBIDDEN");

        if (string.Equals(order.status, OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Cancelled order cannot be updated", "ORDER_ALREADY_CANCELLED");

        var address = await _db.Address
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.id == addressId && x.userId == buyerId, ct);

        if (address == null)
            throw new NotFoundException("Address not found", "ADDRESS_NOT_FOUND");

        order.addressId = addressId;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(buyerId, orderId, ct);
    }

    public async Task<OrderDetailDto> PayAsync(int buyerId, int orderId, CancellationToken ct)
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

        var payment = order.Payment
            .OrderByDescending(x => x.id)
            .FirstOrDefault();

        if (payment == null)
            throw new NotFoundException("Payment record not found", "PAYMENT_NOT_FOUND");

        if (string.Equals(payment.status, PaymentStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            return await GetByIdAsync(buyerId, orderId, ct);

        payment.status = PaymentStatuses.Paid;
        payment.paidAt = DateTime.UtcNow;
        order.status = OrderStatuses.Paid;

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
            totalPrice: order.totalPrice ?? 0m,
            status: order.status,
            totalItems: order.OrderItem.Sum(x => x.quantity ?? 0),
            items: items,
            payments: payments
        );
    }

    private static OrderDetailDto MapDetail(OrderTable order)
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