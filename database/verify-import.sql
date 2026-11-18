:On Error exit

USE [AnphaHealth];
GO

SET NOCOUNT ON;

SELECT ContentKind AS [Kind], COUNT(*) AS [Rows]
FROM dbo.ContentPages
GROUP BY ContentKind
ORDER BY ContentKind;

SELECT
    (SELECT COUNT(*) FROM dbo.ContentPages) AS ContentPages,
    (SELECT COUNT(*) FROM dbo.MediaAssets) AS MediaAssets,
    (SELECT COUNT(*) FROM dbo.ContentPageMedia) AS PageMediaLinks,
    (SELECT COUNT(*) FROM dbo.ProductCategories) AS Categories,
    (SELECT COUNT(*) FROM dbo.Products) AS Products,
    (SELECT COUNT(*) FROM dbo.SiteSettings) AS Settings,
    (SELECT COUNT(*) FROM dbo.SourceSnapshots) AS SourceSnapshots;

SELECT CAST(Slug AS nvarchar(50)) AS Slug, PublishedAt, FeaturedImageId
FROM dbo.ContentPages
WHERE Slug IN (N'tim-hieu-ve-tui-nang-nguc', N'gioi-thieu-ve-tap-doan-gca-nagor')
ORDER BY PublishedAt DESC;

SELECT CAST(JSON_VALUE(SourceMetadata, '$.wordpress.type') AS nvarchar(20)) AS WordPressType, COUNT(*) AS Records
FROM dbo.ContentPages
WHERE JSON_VALUE(SourceMetadata, '$.wordpress.type') IS NOT NULL
GROUP BY JSON_VALUE(SourceMetadata, '$.wordpress.type')
ORDER BY WordPressType;

DECLARE @InvalidHashes int =
(
    SELECT COUNT(*)
    FROM dbo.SourceSnapshots
    WHERE CONVERT(char(64), HASHBYTES('SHA2_256', Payload), 2) <> Sha256
);

SELECT
    COUNT(*) AS Snapshots,
    SUM(CONVERT(bigint, DATALENGTH(Payload))) AS ArchivedBytes,
    @InvalidHashes AS InvalidHashes
FROM dbo.SourceSnapshots;

IF @InvalidHashes > 0
    THROW 50002, 'One or more archived sources failed SHA-256 verification.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.MediaAssets AS asset
    JOIN dbo.SourceSnapshots AS snapshot
        ON snapshot.RelativePath = N'src/AnphaHealth.Web/wwwroot' + asset.LocalPath
    WHERE asset.ByteLength <> DATALENGTH(snapshot.Payload)
)
    THROW 50003, 'A crawled asset size does not match its archived data.', 1;
GO
