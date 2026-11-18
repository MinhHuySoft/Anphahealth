using AnphaHealth.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Data;

public partial class AnphaHealthDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentPage>().Property(page => page.UpdatedAt).HasPrecision(7).IsConcurrencyToken();
        modelBuilder.Entity<Product>().Property(product => product.UpdatedAt).HasPrecision(7).IsConcurrencyToken();
    }
}
