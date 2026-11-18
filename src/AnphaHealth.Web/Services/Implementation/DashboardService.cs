using AnphaHealth.Web.Data;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class DashboardService(IDbContextFactory<AnphaHealthDbContext> factory, CmsAccess access)
    : IDashboardService
{
    public async Task<DashboardStats> GetAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var counts = await context.ContentPages.GroupBy(row => row.IsPublished)
            .Select(group => new { Published = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var products = await context.Products.CountAsync(cancellationToken);
        var media = await context.MediaAssets.CountAsync(cancellationToken);
        var unread = await context.ContactRequests.CountAsync(row => row.Status == "Unread", cancellationToken);
        var recent = await context.ContentPages.AsNoTracking().OrderByDescending(row => row.UpdatedAt)
            .ThenByDescending(row => row.Id).Take(6)
            .Select(row => new ContentListItem(row.Id, row.Title, row.Slug, row.ContentKind, row.IsPublished,
                row.UpdatedAt, row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null))
            .ToListAsync(cancellationToken);
        return new DashboardStats(counts.Sum(x => x.Count), counts.Where(x => x.Published).Sum(x => x.Count),
            products, media, unread, recent);
    }
}
