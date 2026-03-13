using System;
using System.Collections.Generic;

namespace CloneEbay.Domain.Entities;

public partial class Category
{
    public int id { get; set; }

    public string? name { get; set; }

    public virtual ICollection<Product> Product { get; set; } = new List<Product>();
}
