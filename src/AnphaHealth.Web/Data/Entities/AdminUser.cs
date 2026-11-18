using System;
using System.Collections.Generic;

namespace AnphaHealth.Web.Data.Entities;

public partial class AdminUser
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
