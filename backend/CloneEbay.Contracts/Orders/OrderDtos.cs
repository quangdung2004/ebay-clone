using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Orders;

public record OrderItemSummaryDto(
    int id,
    int? productId,
    string productTitle,
    string? thumbnailUrl,
    int quantity,
    decimal unitPrice,
    decimal lineTotal,
    int? sellerId,
    string? sellerName
);

public record PaymentSummaryDto(
    int id,
    decimal amount,
    string? method,
    string? status,
    DateTime? paidAt
);

public record ShipmentSummaryDto(
    int id,
    int sellerId,
    string? shippingMethod,
    string? carrier,
    string? trackingNumber,
    string? status,
    decimal shippingCost,
    string? currency,
    DateTime? estimatedShipDate,
    DateTime? estimatedDeliveryDate,
    DateTime? shippedAt,
    DateTime? deliveredAt
);

public record TrackingEventDto(
    int id,
    string? statusCode,
    string? description,
    string? location,
    decimal? latitude,
    decimal? longitude,
    DateTime? eventTime
);

public record ShipmentTrackingDto(
    int id,
    int orderId,
    int sellerId,
    string? shippingMethod,
    string? carrier,
    string? trackingNumber,
    string? status,
    decimal shippingCost,
    string? currency,
    DateTime? estimatedShipDate,
    DateTime? estimatedDeliveryDate,
    DateTime? shippedAt,
    DateTime? deliveredAt,
    AddressSummaryDto? originAddress,
    IReadOnlyList<TrackingEventDto> events
);

public record OrderTrackingDto(
    int orderId,
    string? orderStatus,
    string? overallShipmentStatus,
    DateTime? orderDate,
    int totalShipments,
    int deliveredShipments,
    IReadOnlyList<ShipmentTrackingDto> shipments
);

public record AddressSummaryDto(
    int id,
    string? fullName,
    string? phone,
    string? street,
    string? city,
    string? state,
    string? country,
    decimal? latitude,
    decimal? longitude,
    bool? isDefault
);

public record OrderSummaryDto(
    int id,
    int? buyerId,
    string? buyerName,
    int? addressId,
    DateTime? orderDate,
    decimal itemSubtotal,
    decimal shippingTotal,
    decimal discountTotal,
    decimal taxTotal,
    decimal grandTotal,
    decimal totalPrice,
    string? status,
    int totalItems,
    IReadOnlyList<OrderItemSummaryDto> items,
    IReadOnlyList<PaymentSummaryDto> payments
);

public record OrderDetailDto(
    int id,
    string? orderCode,
    int? buyerId,
    string? buyerName,
    DateTime? orderDate,
    decimal itemSubtotal,
    decimal shippingTotal,
    decimal discountTotal,
    decimal taxTotal,
    decimal grandTotal,
    decimal totalPrice,
    string? status,
    AddressSummaryDto? address,
    bool canUpdateAddress,
    int addressChangeCount,
    int remainingAddressChanges,
    IReadOnlyList<OrderItemSummaryDto> items,
    IReadOnlyList<PaymentSummaryDto> payments,
    IReadOnlyList<ShipmentSummaryDto> shipments
);

public record CreateOrderItemRequest(
    [param: Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PositiveNumber)]
    int productId,

    [param: Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PositiveNumber)]
    int quantity
);

public record CreateOrderRequest(
    [param: Required]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string paymentMethod,

    int? addressId,

    [param: Required]
    List<CreateOrderItemRequest> items
);

public record UpdateOrderAddressRequest(
    [param: Range(1, int.MaxValue, ErrorMessage = ValidationMessages.PositiveNumber)]
    int addressId
);

public record UpdateShipmentTrackingRequest(
    [param: Required]
    [param: StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    string status,

    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string? trackingNumber,

    [param: StringLength(255, ErrorMessage = ValidationMessages.MaxLength)]
    string? description,

    [param: StringLength(255, ErrorMessage = ValidationMessages.MaxLength)]
    string? location,

    [param: Range(-90d, 90d, ErrorMessage = "Latitude must be between -90 and 90")]
    decimal? latitude,

    [param: Range(-180d, 180d, ErrorMessage = "Longitude must be between -180 and 180")]
    decimal? longitude,

    DateTime? eventTime
);

public record ConfirmShipmentHandlingRequest(
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string? trackingNumber,

    [param: StringLength(255, ErrorMessage = ValidationMessages.MaxLength)]
    string? note,

    DateTime? handlingAt,

    DateTime? estimatedShipDate,

    DateTime? estimatedDeliveryDate
);

public record QuoteOrderRequest(
    int? addressId,

    [param: Required]
    List<CreateOrderItemRequest> items
);

public record QuoteOrderDto(
    decimal itemSubtotal,
    decimal shippingTotal,
    decimal discountTotal,
    decimal taxTotal,
    decimal grandTotal,
    IReadOnlyList<ShipmentSummaryDto> shipments
);

public record SellerShipmentSummaryDto(
    int shipmentId,
    int orderId,
    string? orderCode,
    string? orderStatus,
    string? shipmentStatus,
    string? trackingNumber,
    DateTime? orderDate,
    DateTime? shippedAt,
    DateTime? estimatedDeliveryDate,
    string? buyerName,
    string? destinationLabel,
    decimal shippingFee,
    int totalItems,
    IReadOnlyList<OrderItemSummaryDto> items
);