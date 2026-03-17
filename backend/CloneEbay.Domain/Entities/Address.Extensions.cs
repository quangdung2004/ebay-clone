namespace CloneEbay.Domain.Entities;

public partial class Address
{
    public string? addressType { get; set; }

    public bool? isShippingOrigin { get; set; }
}