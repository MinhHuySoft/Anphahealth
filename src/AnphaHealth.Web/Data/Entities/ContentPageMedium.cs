using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class ContentPageMedium
{
    public int ContentPageId { get; set; }

    public int MediaAssetId { get; set; }

    public int SortOrder { get; set; }

    public virtual ContentPage ContentPage { get; set; } = null!;

    public virtual MediaAsset MediaAsset { get; set; } = null!;
}
