using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class ProductCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public string Icon { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
