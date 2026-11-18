using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class Product
{
    public int Id { get; set; }

    public int ContentPageId { get; set; }

    public int ProductCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? FeaturedImageId { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsPublished { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ContentPage ContentPage { get; set; } = null!;

    public virtual MediaAsset? FeaturedImage { get; set; }

    public virtual ProductCategory ProductCategory { get; set; } = null!;
}
