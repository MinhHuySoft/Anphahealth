namespace AnphaHealth.Web.Services.Contracts;

public interface IPublicWebsiteService
{
    Task<PublicPage?> GetPageAsync(string slug, string? kind = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArchiveEntry>> ListArchiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArticleSummary>> ListArticlesAsync(int? limit = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublicProduct>> ListProductsAsync(CancellationToken cancellationToken = default);
    string Asset(string fileName);
    string Route(string slug, string? kind = null);
}
