SET NOCOUNT ON;
SET XACT_ABORT ON;

INSERT dbo.MediaAssets (SourceUrl, LocalPath, FileName, ContentType, ByteLength)
SELECT seed.SourceUrl, seed.LocalPath, seed.FileName, seed.ContentType, seed.ByteLength
FROM OPENJSON(@MediaJson) WITH
(
    SourceUrl nvarchar(1000) '$.url',
    LocalPath nvarchar(450) '$.local',
    FileName nvarchar(260) '$.filename',
    ContentType nvarchar(150) '$.type',
    ByteLength bigint '$.bytes'
) AS seed
WHERE NOT EXISTS (SELECT 1 FROM dbo.MediaAssets AS asset WHERE asset.LocalPath = seed.LocalPath);

INSERT dbo.ContentPages
(
    Slug, Title, Html, PlainText, SourceUrl, RawFile, ContentKind,
    IsPublished, Summary, FeaturedImageId, PublishedAt, SourceMetadata
)
SELECT seed.Slug, seed.Title, seed.Html, seed.PlainText, seed.SourceUrl, seed.RawFile,
    seed.ContentKind, 1, seed.Summary, asset.Id, seed.PublishedAt, seed.SourceMetadata
FROM OPENJSON(@PagesJson) WITH
(
    Slug nvarchar(450) '$.slug',
    Title nvarchar(250) '$.title',
    Html nvarchar(max) '$.html',
    PlainText nvarchar(max) '$.text',
    SourceUrl nvarchar(1000) '$.url',
    RawFile nvarchar(500) '$.rawFile',
    ContentKind nvarchar(30) '$.kind',
    Summary nvarchar(500) '$.summary',
    ImagePath nvarchar(450) '$.imagePath',
    PublishedAt datetime2(0) '$.publishedAt',
    SourceMetadata nvarchar(max) '$.sourceMetadata' AS JSON
) AS seed
LEFT JOIN dbo.MediaAssets AS asset ON asset.LocalPath = seed.ImagePath
WHERE NOT EXISTS (SELECT 1 FROM dbo.ContentPages AS page WHERE page.Slug = seed.Slug);

INSERT dbo.ContentPageMedia (ContentPageId, MediaAssetId, SortOrder)
SELECT page.Id, asset.Id, MIN(CONVERT(int, image.[key]))
FROM OPENJSON(@PagesJson) WITH (Slug nvarchar(450) '$.slug', Images nvarchar(max) '$.localImages' AS JSON) AS seed
JOIN dbo.ContentPages AS page ON page.Slug = seed.Slug
CROSS APPLY OPENJSON(seed.Images) AS image
JOIN dbo.MediaAssets AS asset ON asset.LocalPath = image.[value] COLLATE DATABASE_DEFAULT
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.ContentPageMedia AS link
    WHERE link.ContentPageId = page.Id AND link.MediaAssetId = asset.Id
)
GROUP BY page.Id, asset.Id;

INSERT dbo.ProductCategories (Name, Slug, DisplayOrder, Icon)
SELECT seed.Name, seed.Slug, seed.DisplayOrder, seed.Icon
FROM OPENJSON(@CatalogJson, '$.categories') WITH
(
    Name nvarchar(150) '$.name',
    Slug nvarchar(120) '$.slug',
    DisplayOrder int '$.displayOrder',
    Icon nvarchar(45) '$.icon'
) AS seed
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductCategories AS category WHERE category.Slug = seed.Slug);

INSERT dbo.Products
(
    ContentPageId, ProductCategoryId, Name, Brand, Description,
    FeaturedImageId, IsFeatured, IsPublished, DisplayOrder
)
SELECT page.Id, category.Id, seed.Name, seed.Brand, seed.Description,
    asset.Id, seed.IsFeatured, 1, seed.DisplayOrder
FROM OPENJSON(@CatalogJson, '$.products') WITH
(
    PageSlug nvarchar(450) '$.slug',
    CategorySlug nvarchar(120) '$.category',
    Name nvarchar(250) '$.name',
    Brand nvarchar(150) '$.brand',
    Description nvarchar(500) '$.description',
    ImagePath nvarchar(450) '$.imagePath',
    IsFeatured bit '$.isFeatured',
    DisplayOrder int '$.displayOrder'
) AS seed
JOIN dbo.ContentPages AS page ON page.Slug = seed.PageSlug
JOIN dbo.ProductCategories AS category ON category.Slug = seed.CategorySlug
JOIN dbo.MediaAssets AS asset ON asset.LocalPath = seed.ImagePath
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS product WHERE product.ContentPageId = page.Id);

INSERT dbo.SiteSettings (SettingKey, SettingValue)
SELECT seed.[key], seed.[value]
FROM OPENJSON(@CatalogJson, '$.settings') AS seed
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.SiteSettings AS setting
    WHERE setting.SettingKey = seed.[key] COLLATE DATABASE_DEFAULT
);
