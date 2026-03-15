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

public record ShippingSummaryDto(
    int id,
    string? carrier,
    string? trackingNumber,
    string? status,
    DateTime? estimatedArrival
);

public record AddressSummaryDto(
    int id,
    string? fullName,
    string? phone,
    string? street,
    string? city,
    string? state,
    string? country,
    bool? isDefault
);

public record OrderSummaryDto(
    int id,
    int? buyerId,
    string? buyerName,
    int? addressId,
    DateTime? orderDate,
    decimal totalPrice,
    string? status,
    int totalItems,
    IReadOnlyList<OrderItemSummaryDto> items,
    IReadOnlyList<PaymentSummaryDto> payments
);

public record OrderDetailDto(
    int id,
    int? buyerId,
    string? buyerName,
    DateTime? orderDate,
    decimal totalPrice,
    string? status,
    AddressSummaryDto? address,
    IReadOnlyList<OrderItemSummaryDto> items,
    IReadOnlyList<PaymentSummaryDto> payments,
    IReadOnlyList<ShippingSummaryDto> shippings
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