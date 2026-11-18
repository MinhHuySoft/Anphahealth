using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class MediaAsset
{
    public int Id { get; set; }

    public string? SourceUrl { get; set; }

    public string LocalPath { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long ByteLength { get; set; }

    public string AltText { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ContentPageMedium> ContentPageMedia { get; set; } = new List<ContentPageMedium>();

    public virtual ICollection<ContentPage> ContentPages { get; set; } = new List<ContentPage>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
