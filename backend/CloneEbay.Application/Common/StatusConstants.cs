namespace CloneEbay.Application.Common;

public static class OrderStatuses
{
    public const string PendingPayment = "PENDING_PAYMENT";
    public const string Paid = "PAID";
    public const string Processing = "PROCESSING";
    public const string Shipped = "SHIPPED";
    public const string Delivered = "DELIVERED";
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
}

public static class PaymentStatuses
{
    public const string Pending = "PENDING";
    public const string Captured = "CAPTURED";
    public const string Cancelled = "CANCELLED";
}

public static class PaymentMethods
{
    public const string Cod = "COD";
    public const string PayPal = "PAYPAL";
}

public static class SettlementStatuses
{
    public const string Pending = "PENDING";
    public const string OnHold = "ON_HOLD";
    public const string Available = "AVAILABLE";
    public const string PaidOut = "PAID_OUT";
    public const string Reversed = "REVERSED";
}

public static class ShipmentStatuses
{
    public const string Pending = "PENDING";
    public const string LabelCreated = "LABEL_CREATED";
    public const string InTransit = "IN_TRANSIT";
    public const string OutForDelivery = "OUT_FOR_DELIVERY";
    public const string Delivered = "DELIVERED";
    public const string Exception = "EXCEPTION";
}

public static class ProductStatuses
{
    public const string Draft = "DRAFT";
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string OutOfStock = "OUT_OF_STOCK";
    public const string Ended = "ENDED";
    public const string Sold = "SOLD";
}

public static class ProductConditions
{
    public const string New = "NEW";
    public const string Used = "USED";
    public const string Refurbished = "REFURBISHED";
    public const string OpenBox = "OPEN_BOX";
    public const string ForParts = "FOR_PARTS";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        New,
        Used,
        Refurbished,
        OpenBox,
        ForParts
    };
}
