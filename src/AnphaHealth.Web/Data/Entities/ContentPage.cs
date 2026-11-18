using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class ContentPage
{
    public int Id { get; set; }

    public string Slug { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Html { get; set; } = null!;

    public string PlainText { get; set; } = null!;

    public string? SourceUrl { get; set; }

    public string? RawFile { get; set; }

    public string ContentKind { get; set; } = null!;

    public bool IsPublished { get; set; }

    public string Summary { get; set; } = null!;

    public int? FeaturedImageId { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? SourceMetadata { get; set; }

    public virtual ICollection<ContentPageMedium> ContentPageMedia { get; set; } = new List<ContentPageMedium>();

    public virtual MediaAsset? FeaturedImage { get; set; }

    public virtual Product? Product { get; set; }
}
