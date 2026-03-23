namespace CloneEbay.Contracts.Orders;

public record MyCouponDto(
    int userCouponId,
    int couponId,
    string code,
    decimal discountPercent,
    int quantity,
    DateTime? startDate,
    DateTime? endDate,
    int? productId
);