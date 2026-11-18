using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class SiteSetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
