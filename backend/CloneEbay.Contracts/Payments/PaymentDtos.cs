using System.ComponentModel.DataAnnotations;
using CloneEbay.Contracts.Orders;
using CloneEbay.Contracts.Validation;

namespace CloneEbay.Contracts.Payments;

public record CreatePayPalPaymentDto(
    string paypalOrderId,
    string status,
    string? approveUrl
);

public record CapturePayPalPaymentRequest(
    [param: RequiredTrimmed]
    [param: StringLength(100, ErrorMessage = ValidationMessages.MaxLength)]
    string paypalOrderId
);