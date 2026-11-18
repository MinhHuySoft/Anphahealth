using AnphaHealth.Web.Data;
using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal static class ContentWriter
{
    public static async Task ApplyAsync(AnphaHealthDbContext context, ContentPage page, ContentInput input,
        string slug, CancellationToken cancellationToken)
    {
        await CmsValidation.CheckImageAsync(context, input.FeaturedImageId, cancellationToken);
        if (await context.ContentPages.AnyAsync(row => row.Slug == slug && row.Id != page.Id, cancellationToken))
            throw new CmsException("Đường dẫn này đã được sử dụng.");

        if (page.Id == 0 || !string.Equals(input.Html, page.Html, StringComparison.Ordinal))
        {
            var cleaned = ContentHtml.Sanitize(input.Html);
            page.Html = cleaned.Html;
            page.PlainText = cleaned.Text;
            var mediaIds = await context.MediaAssets
                .Where(item => cleaned.ImagePaths.Contains(item.LocalPath))
                .Select(item => new { item.Id, item.LocalPath })
                .ToDictionaryAsync(item => item.LocalPath, item => item.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var removed = page.ContentPageMedia.Where(item => !mediaIds.ContainsValue(item.MediaAssetId)).ToArray();
            foreach (var link in removed)
                page.ContentPageMedia.Remove(link);

            for (var index = 0; index < cleaned.ImagePaths.Count; index++)
            {
                if (!mediaIds.TryGetValue(cleaned.ImagePaths[index], out var id))
                    continue;

                var link = page.ContentPageMedia.FirstOrDefault(link => link.MediaAssetId == id);
                if (link is null)
                    page.ContentPageMedia.Add(new ContentPageMedium { MediaAssetId = id, SortOrder = index });
                else
                    link.SortOrder = index;
            }
        }

        page.Title = input.Title.Trim();
        page.Slug = slug;
        page.ContentKind = input.Kind;
        page.Summary = input.Summary.Trim();
        page.FeaturedImageId = input.FeaturedImageId;
        page.IsPublished = input.IsPublished;
        page.UpdatedAt = DateTime.UtcNow;
        if (page.IsPublished)
            page.PublishedAt ??= page.UpdatedAt;
    }
}
