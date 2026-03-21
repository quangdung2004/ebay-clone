using System;

namespace CloneEbay.Domain.Entities;

public partial class Address
{
    public decimal? latitude { get; set; }

    public decimal? longitude { get; set; }
}