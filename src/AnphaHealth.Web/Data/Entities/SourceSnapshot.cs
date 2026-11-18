using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class SourceSnapshot
{
    public int Id { get; set; }

    public string RelativePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public byte[] Payload { get; set; } = null!;

    public string Sha256 { get; set; } = null!;

    public DateTime ImportedAt { get; set; }
}
