using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using DbContentPage = AnphaHealth.Web.Data.Entities.ContentPage;
using DbProduct = AnphaHealth.Web.Data.Entities.Product;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class PublicWebsiteService(IRepository<DbContentPage> pages, IRepository<DbProduct> products,
    StaticMediaLibrary library) : IPublicWebsiteService
{
    public Task<PublicPage?> GetPageAsync(string slug, string? kind = null,
        CancellationToken cancellationToken = default)
    {
        slug = slug.Trim('/');
        return pages.ReadAsync((rows, token) => rows
            .Where(row => row.IsPublished && row.Slug == slug && (kind == null || row.ContentKind == kind))
            .Select(row => new PublicPage(row.Slug, row.Title, row.Html, row.SourceUrl, row.Summary,
                row.ContentKind, row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null,
                row.PublishedAt, row.ContentPageMedia.OrderBy(link => link.SortOrder)
                    .Select(link => link.MediaAsset.LocalPath).ToList()))
            .SingleOrDefaultAsync(token), cancellationToken);
    }

    public async Task<IReadOnlyList<ArchiveEntry>> ListArchiveAsync(CancellationToken cancellationToken = default) =>
        await pages.ReadAsync((rows, token) => rows.Where(row => row.IsPublished).OrderBy(row => row.Slug)
            .Select(row => new ArchiveEntry(row.Slug, row.Title, row.ContentKind)).ToListAsync(token), cancellationToken);

    public async Task<IReadOnlyList<ArticleSummary>> ListArticlesAsync(int? limit = null,
        CancellationToken cancellationToken = default) =>
        await pages.ReadAsync((rows, token) =>
        {
            IQueryable<DbContentPage> articles = rows.Where(row => row.IsPublished && row.ContentKind == "Article")
                .OrderByDescending(row => row.PublishedAt).ThenByDescending(row => row.Id);
            if (limit.HasValue)
                articles = articles.Take(limit.Value);

            return articles.Select(row => new ArticleSummary(row.Slug, row.Title, row.Summary,
                row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null, row.PublishedAt)).ToListAsync(token);
        }, cancellationToken);

    public async Task<IReadOnlyList<PublicProduct>> ListProductsAsync(CancellationToken cancellationToken = default) =>
        await products.ReadAsync((rows, token) => rows.Where(row => row.IsPublished && row.ContentPage.IsPublished)
            .OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id)
            .Select(row => new PublicProduct(row.ContentPage.Slug, row.Brand, row.ProductCategory.Name, row.Name,
                row.Description, row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null,
                row.ProductCategory.Icon, row.IsFeatured))
            .ToListAsync(token), cancellationToken);

    public string Asset(string fileName) => library.Resolve(fileName);

    public string Route(string slug, string? kind = null) => kind switch
    {
        "Article" => "/tin-tuc/" + slug,
        "Product" => "/san-pham/" + slug,
        _ => slug switch
        {
            "home" => "/",
            "gioi-thieu" => "/gioi-thieu",
            "san-pham-page" => "/san-pham",
            "tin-tuc" => "/tin-tuc",
            "lien-he" => "/lien-he",
            "hinh-anh-cong-ty" or "hoat-dong-xa-hoi" => "/hoat-dong",
            "tin-tuyen-dung" or "chinh-sach-cong-ty" or "ung-tuyen" => "/tuyen-dung",
            _ => "/noi-dung/" + slug
        }
    };
}

internal sealed class StaticMediaLibrary(IWebHostEnvironment environment)
{
    private readonly string[] files = Directory.EnumerateFiles(Path.Combine(environment.WebRootPath, "media"))
        .Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray();

    public string Resolve(string fileName) =>
        "/media/" + (files.FirstOrDefault(file => file.EndsWith("-" + fileName, StringComparison.OrdinalIgnoreCase)) ?? fileName);
}
