using System;

namespace CloneEbay.Domain.Entities;

public partial class Product
{
    public string? status { get; set; }

    public string? condition { get; set; }

    public int? viewCount { get; set; }

    public bool? isDeleted { get; set; }

    public DateTime? deletedAt { get; set; }

    public int? weightGrams { get; set; }

    public decimal? lengthCm { get; set; }

    public decimal? widthCm { get; set; }

    public decimal? heightCm { get; set; }

    public int? handlingDays { get; set; }
}