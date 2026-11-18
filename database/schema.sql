:On Error exit

USE [master];
GO

SET NOCOUNT ON;
DECLARE @CreateLockResult int;
EXEC @CreateLockResult = sys.sp_getapplock
    @Resource = N'AnphaHealth.CreateDatabase', @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 15000;
IF @CreateLockResult < 0
    THROW 50000, 'Cannot acquire the database creation lock.', 1;

IF DB_ID(N'AnphaHealth') IS NULL
BEGIN
    CREATE DATABASE [AnphaHealth] COLLATE Latin1_General_100_CI_AS_SC;
END;

EXEC sys.sp_releaseapplock @Resource = N'AnphaHealth.CreateDatabase', @LockOwner = 'Session';
GO

USE [AnphaHealth];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @SchemaLockResult int;
EXEC @SchemaLockResult = sys.sp_getapplock
    @Resource = N'AnphaHealth.ApplySchema', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
IF @SchemaLockResult < 0
    THROW 50000, 'Cannot acquire the schema installation lock.', 1;

IF OBJECT_ID(N'dbo.MediaAssets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MediaAssets
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_MediaAssets PRIMARY KEY,
        SourceUrl nvarchar(1000) NULL,
        LocalPath nvarchar(450) NOT NULL CONSTRAINT UQ_MediaAssets_LocalPath UNIQUE,
        FileName nvarchar(260) NOT NULL,
        ContentType nvarchar(150) NOT NULL,
        ByteLength bigint NOT NULL CONSTRAINT CK_MediaAssets_ByteLength CHECK (ByteLength >= 0),
        AltText nvarchar(250) NOT NULL CONSTRAINT DF_MediaAssets_AltText DEFAULT N'',
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_MediaAssets_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'dbo.ContentPages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContentPages
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ContentPages PRIMARY KEY,
        Slug nvarchar(450) NOT NULL CONSTRAINT UQ_ContentPages_Slug UNIQUE,
        Title nvarchar(250) NOT NULL,
        Html nvarchar(max) NOT NULL CONSTRAINT DF_ContentPages_Html DEFAULT N'',
        PlainText nvarchar(max) NOT NULL CONSTRAINT DF_ContentPages_PlainText DEFAULT N'',
        SourceUrl nvarchar(1000) NULL,
        RawFile nvarchar(500) NULL,
        ContentKind nvarchar(30) NOT NULL CONSTRAINT CK_ContentPages_ContentKind CHECK (ContentKind IN (N'Article', N'Page', N'Product', N'Archive')),
        IsPublished bit NOT NULL CONSTRAINT DF_ContentPages_IsPublished DEFAULT (0),
        Summary nvarchar(500) NOT NULL CONSTRAINT DF_ContentPages_Summary DEFAULT N'',
        FeaturedImageId int NULL CONSTRAINT FK_ContentPages_FeaturedImage REFERENCES dbo.MediaAssets (Id),
        PublishedAt datetime2(0) NULL,
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_ContentPages_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_ContentPages_UpdatedAt DEFAULT SYSUTCDATETIME(),
        SourceMetadata nvarchar(max) NULL CONSTRAINT CK_ContentPages_SourceMetadata CHECK (SourceMetadata IS NULL OR ISJSON(SourceMetadata) = 1)
    );
    CREATE INDEX IX_ContentPages_Listing ON dbo.ContentPages (ContentKind, IsPublished, PublishedAt DESC, Id DESC);
    CREATE INDEX IX_ContentPages_FeaturedImageId ON dbo.ContentPages (FeaturedImageId);
END;

IF OBJECT_ID(N'dbo.ContentPageMedia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContentPageMedia
    (
        ContentPageId int NOT NULL CONSTRAINT FK_ContentPageMedia_ContentPage REFERENCES dbo.ContentPages (Id) ON DELETE CASCADE,
        MediaAssetId int NOT NULL CONSTRAINT FK_ContentPageMedia_MediaAsset REFERENCES dbo.MediaAssets (Id),
        SortOrder int NOT NULL CONSTRAINT DF_ContentPageMedia_SortOrder DEFAULT (0),
        CONSTRAINT PK_ContentPageMedia PRIMARY KEY (ContentPageId, MediaAssetId)
    );
    CREATE INDEX IX_ContentPageMedia_MediaAssetId ON dbo.ContentPageMedia (MediaAssetId);
END;

IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductCategories
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ProductCategories PRIMARY KEY,
        Name nvarchar(150) NOT NULL,
        Slug nvarchar(120) NOT NULL CONSTRAINT UQ_ProductCategories_Slug UNIQUE,
        DisplayOrder int NOT NULL CONSTRAINT DF_ProductCategories_DisplayOrder DEFAULT (0),
        Icon nvarchar(45) NOT NULL CONSTRAINT DF_ProductCategories_Icon DEFAULT N'cross'
    );
END;

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        ContentPageId int NOT NULL CONSTRAINT UQ_Products_ContentPageId UNIQUE
            CONSTRAINT FK_Products_ContentPage REFERENCES dbo.ContentPages (Id) ON DELETE CASCADE,
        ProductCategoryId int NOT NULL CONSTRAINT FK_Products_ProductCategory REFERENCES dbo.ProductCategories (Id),
        Name nvarchar(250) NOT NULL,
        Brand nvarchar(150) NOT NULL,
        Description nvarchar(500) NOT NULL CONSTRAINT DF_Products_Description DEFAULT N'',
        FeaturedImageId int NULL CONSTRAINT FK_Products_FeaturedImage REFERENCES dbo.MediaAssets (Id),
        IsFeatured bit NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT (0),
        IsPublished bit NOT NULL CONSTRAINT DF_Products_IsPublished DEFAULT (0),
        DisplayOrder int NOT NULL CONSTRAINT DF_Products_DisplayOrder DEFAULT (0),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(7) NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_Products_CategoryListing ON dbo.Products (ProductCategoryId, IsPublished, DisplayOrder);
    CREATE INDEX IX_Products_FeaturedImageId ON dbo.Products (FeaturedImageId);
END;

IF OBJECT_ID(N'dbo.SiteSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiteSettings
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_SiteSettings PRIMARY KEY,
        SettingKey nvarchar(100) NOT NULL CONSTRAINT UQ_SiteSettings_SettingKey UNIQUE,
        SettingValue nvarchar(2000) NOT NULL,
        UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_SiteSettings_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminUsers
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AdminUsers PRIMARY KEY,
        UserName nvarchar(80) NOT NULL CONSTRAINT UQ_AdminUsers_UserName UNIQUE,
        PasswordHash nvarchar(512) NOT NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AdminUsers_IsActive DEFAULT (1),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_AdminUsers_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'dbo.ContactRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactRequests
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ContactRequests PRIMARY KEY,
        Name nvarchar(120) NOT NULL,
        Email nvarchar(254) NOT NULL,
        Phone nvarchar(40) NULL,
        Interest nvarchar(200) NOT NULL,
        Message nvarchar(4000) NOT NULL,
        Status nvarchar(24) NOT NULL CONSTRAINT DF_ContactRequests_Status DEFAULT N'Unread',
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_ContactRequests_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ContactRequests_Inbox ON dbo.ContactRequests (Status, CreatedAt DESC);
END;

IF OBJECT_ID(N'dbo.SourceSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SourceSnapshots
    (
        Id int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_SourceSnapshots PRIMARY KEY,
        RelativePath nvarchar(450) NOT NULL CONSTRAINT UQ_SourceSnapshots_RelativePath UNIQUE,
        ContentType nvarchar(150) NOT NULL,
        Payload varbinary(max) NOT NULL,
        Sha256 char(64) NOT NULL,
        ImportedAt datetime2(0) NOT NULL CONSTRAINT DF_SourceSnapshots_ImportedAt DEFAULT SYSUTCDATETIME()
    );
END;


IF COL_LENGTH(N'dbo.ContentPages', N'GuideProductId') IS NULL
BEGIN
    ALTER TABLE dbo.ContentPages ADD
        GuideProductId int NULL CONSTRAINT FK_ContentPages_GuideProduct REFERENCES dbo.Products(Id),
        GuideModel nvarchar(120) NULL,
        GuideFileId int NULL CONSTRAINT FK_ContentPages_GuideFile REFERENCES dbo.MediaAssets(Id),
        IsGuideSample bit NOT NULL CONSTRAINT DF_ContentPages_IsGuideSample DEFAULT (0);
    ALTER TABLE dbo.ContentPages DROP CONSTRAINT CK_ContentPages_ContentKind;
    ALTER TABLE dbo.ContentPages ADD CONSTRAINT CK_ContentPages_ContentKind
        CHECK (ContentKind IN (N'Article', N'Page', N'Product', N'Archive', N'Guide'));
END;

COMMIT TRANSACTION;
GO
