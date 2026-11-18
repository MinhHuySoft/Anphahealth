namespace AnphaHealth.Web.Services;

public sealed record PublicPage(
    string Slug, string Title, string Html, string? Url, string Summary, string Kind,
    string? FeaturedImage, DateTime? PublishedAt, IReadOnlyList<string> LocalImages);

public sealed record PublicProduct(
    string Slug, string Brand, string Category, string Title, string Description, string? Image,
    string Icon, bool Primary);

public sealed record ArchiveEntry(string Slug, string Title, string Kind);

public sealed record ArticleSummary(string Slug, string Title, string Summary, string? Image, DateTime? PublishedAt);
