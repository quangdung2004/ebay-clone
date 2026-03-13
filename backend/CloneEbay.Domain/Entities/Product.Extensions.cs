using System;

namespace CloneEbay.Domain.Entities;

public partial class Product
{
    public string? status { get; set; }

    public string? condition { get; set; }

    public int? viewCount { get; set; }

    public bool? isDeleted { get; set; }

    public DateTime? deletedAt { get; set; }
}