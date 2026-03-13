namespace CloneEbay.Infrastructure.Products;

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