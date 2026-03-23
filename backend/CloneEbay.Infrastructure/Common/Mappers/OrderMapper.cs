using CloneEbay.Contracts.Orders;
using CloneEbay.Domain.Entities;
using CloneEbay.Infrastructure.Products;

namespace CloneEbay.Infrastructure.Common.Mappers;

/// <summary>
/// Shared hand-mapping logic for Order-related entities → DTOs.
/// Used by OrderService, PaymentService, ShippingService to avoid duplication.
/// </summary>
public static class OrderMapper
{
    public static OrderDetailDto ToDetailDto(OrderTable order)
    {
        var subtotal = order.subtotalAmount
            ?? order.OrderItem.Sum(x => (x.unitPrice ?? 0m) * (x.quantity ?? 0));

        var shippingFee = order.shippingFee ?? 0m;
        var totalPrice = order.totalPrice ?? (subtotal + shippingFee);

        return new OrderDetailDto(
            id: order.id,
            buyerId: order.buyerId,
            buyerName: order.buyer?.username,
            orderDate: order.orderDate,
            subtotalAmount: subtotal,
            shippingFee: shippingFee,
            totalPrice: totalPrice,
            status: order.status,
            addressChangeCount: order.addressChangeCount,
            lastAddressChangedAt: order.lastAddressChangedAt,
            address: MapAddress(order.address),
            items: order.OrderItem.Select(ToItemDto).ToList(),
            payments: order.Payment.Select(ToPaymentDto).ToList(),
            shippings: order.ShippingInfo.Select(ToShippingDto).ToList()
        );
    }

    public static OrderSummaryDto ToSummaryDto(OrderTable order)
    {
        var subtotal = order.subtotalAmount
            ?? order.OrderItem.Sum(x => (x.unitPrice ?? 0m) * (x.quantity ?? 0));

        var shippingFee = order.shippingFee ?? 0m;
        var totalPrice = order.totalPrice ?? (subtotal + shippingFee);

        return new OrderSummaryDto(
            id: order.id,
            buyerId: order.buyerId,
            buyerName: order.buyer?.username,
            addressId: order.addressId,
            orderDate: order.orderDate,
            subtotalAmount: subtotal,
            shippingFee: shippingFee,
            totalPrice: totalPrice,
            status: order.status,
            addressChangeCount: order.addressChangeCount,
            lastAddressChangedAt: order.lastAddressChangedAt,
            totalItems: order.OrderItem.Sum(x => x.quantity ?? 0),
            items: order.OrderItem.Select(ToItemDto).ToList(),
            payments: order.Payment.Select(ToPaymentDto).ToList()
        );
    }

    public static OrderItemSummaryDto ToItemDto(OrderItem item)
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

    public static PaymentSummaryDto ToPaymentDto(Payment payment)
    {
        return new PaymentSummaryDto(
            id: payment.id,
            amount: payment.amount ?? 0m,
            method: payment.method,
            status: payment.status,
            paidAt: payment.paidAt
        );
    }

    public static ShippingSummaryDto ToShippingDto(ShippingInfo shipping)
    {
        return new ShippingSummaryDto(
            shipping.id,
            shipping.carrier,
            shipping.trackingNumber,
            shipping.status,
            shipping.estimatedArrival,
            shipping.shippedAt,
            shipping.deliveredAt,
            shipping.provider,
            shipping.lastCheckpoint,
            shipping.lastCheckpointTime,
            shipping.ShippingTrackingEvent?.Select(e => new TrackingEventDto(
                e.mainStatus,
                e.subStatus,
                e.description,
                e.location,
                e.eventTime,
                e.latitude,
                e.longitude,
                e.geocodeStatus
            )).OrderByDescending(e => e.eventTime).ToList()
        );
    }

    private static AddressSummaryDto? MapAddress(Address? address)
    {
        if (address == null) return null;

        return new AddressSummaryDto(
            address.id,
            address.fullName,
            address.phone,
            address.street,
            address.city,
            address.state,
            address.country,
            address.isDefault
        );
    }
}
