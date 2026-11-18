namespace AnphaHealth.Web.Services;

public sealed record PublicGuide(string Slug, string Title, string Summary, string Model,
    string ProductSlug, string ProductName, string Brand, string Category, bool IsSample, string? FilePath, string? ProductImage);
