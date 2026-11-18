using System;
using System.Collections.Generic;
using AnphaHealth.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Data;

public partial class AnphaHealthDbContext : DbContext
{
    public AnphaHealthDbContext(DbContextOptions<AnphaHealthDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<ContactRequest> ContactRequests { get; set; }

    public virtual DbSet<ContentPage> ContentPages { get; set; }

    public virtual DbSet<ContentPageMedium> ContentPageMedia { get; set; }

    public virtual DbSet<MediaAsset> MediaAssets { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<SiteSetting> SiteSettings { get; set; }

    public virtual DbSet<SourceSnapshot> SourceSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_100_CI_AS_SC");

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasIndex(e => e.UserName, "UQ_AdminUsers_UserName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AdminUsers_CreatedAt");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_AdminUsers_IsActive");
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.UserName).HasMaxLength(80);
        });

        modelBuilder.Entity<ContactRequest>(entity =>
        {
            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_ContactRequests_Inbox").IsDescending(false, true);

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ContactRequests_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.Interest).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(4000);
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.Property(e => e.Phone).HasMaxLength(40);
            entity.Property(e => e.Status)
                .HasMaxLength(24)
                .HasDefaultValue("Unread", "DF_ContactRequests_Status");
        });

        modelBuilder.Entity<ContentPage>(entity =>
        {
            entity.HasIndex(e => e.FeaturedImageId, "IX_ContentPages_FeaturedImageId");

            entity.HasIndex(e => new { e.ContentKind, e.IsPublished, e.PublishedAt, e.Id }, "IX_ContentPages_Listing").IsDescending(false, false, true, true);

            entity.HasIndex(e => e.Slug, "UQ_ContentPages_Slug").IsUnique();

            entity.Property(e => e.ContentKind).HasMaxLength(30);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ContentPages_CreatedAt");
            entity.Property(e => e.GuideModel).HasMaxLength(120);
            entity.Property(e => e.Html).HasDefaultValue("", "DF_ContentPages_Html");
            entity.Property(e => e.PlainText).HasDefaultValue("", "DF_ContentPages_PlainText");
            entity.Property(e => e.PublishedAt).HasPrecision(0);
            entity.Property(e => e.RawFile).HasMaxLength(500);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
            entity.Property(e => e.Summary)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_ContentPages_Summary");
            entity.Property(e => e.Title).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_ContentPages_UpdatedAt");

            entity.HasOne(d => d.FeaturedImage).WithMany(p => p.ContentPageFeaturedImages)
                .HasForeignKey(d => d.FeaturedImageId)
                .HasConstraintName("FK_ContentPages_FeaturedImage");

            entity.HasOne(d => d.GuideFile).WithMany(p => p.ContentPageGuideFiles)
                .HasForeignKey(d => d.GuideFileId)
                .HasConstraintName("FK_ContentPages_GuideFile");

            entity.HasOne(d => d.GuideProduct).WithMany(p => p.ContentPages)
                .HasForeignKey(d => d.GuideProductId)
                .HasConstraintName("FK_ContentPages_GuideProduct");
        });

        modelBuilder.Entity<ContentPageMedium>(entity =>
        {
            entity.HasKey(e => new { e.ContentPageId, e.MediaAssetId });

            entity.HasIndex(e => e.MediaAssetId, "IX_ContentPageMedia_MediaAssetId");

            entity.HasOne(d => d.ContentPage).WithMany(p => p.ContentPageMedia)
                .HasForeignKey(d => d.ContentPageId)
                .HasConstraintName("FK_ContentPageMedia_ContentPage");

            entity.HasOne(d => d.MediaAsset).WithMany(p => p.ContentPageMedia)
                .HasForeignKey(d => d.MediaAssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContentPageMedia_MediaAsset");
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasIndex(e => e.LocalPath, "UQ_MediaAssets_LocalPath").IsUnique();

            entity.Property(e => e.AltText)
                .HasMaxLength(250)
                .HasDefaultValue("", "DF_MediaAssets_AltText");
            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_MediaAssets_CreatedAt");
            entity.Property(e => e.FileName).HasMaxLength(260);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => new { e.ProductCategoryId, e.IsPublished, e.DisplayOrder }, "IX_Products_CategoryListing");

            entity.HasIndex(e => e.FeaturedImageId, "IX_Products_FeaturedImageId");

            entity.HasIndex(e => e.ContentPageId, "UQ_Products_ContentPageId").IsUnique();

            entity.Property(e => e.Brand).HasMaxLength(150);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Products_CreatedAt");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValue("", "DF_Products_Description");
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Products_UpdatedAt");

            entity.HasOne(d => d.ContentPage).WithOne(p => p.Product)
                .HasForeignKey<Product>(d => d.ContentPageId)
                .HasConstraintName("FK_Products_ContentPage");

            entity.HasOne(d => d.FeaturedImage).WithMany(p => p.Products)
                .HasForeignKey(d => d.FeaturedImageId)
                .HasConstraintName("FK_Products_FeaturedImage");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.ProductCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_ProductCategory");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasIndex(e => e.Slug, "UQ_ProductCategories_Slug").IsUnique();

            entity.Property(e => e.Icon)
                .HasMaxLength(45)
                .HasDefaultValue("cross", "DF_ProductCategories_Icon");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Slug).HasMaxLength(120);
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasIndex(e => e.SettingKey, "UQ_SiteSettings_SettingKey").IsUnique();

            entity.Property(e => e.SettingKey).HasMaxLength(100);
            entity.Property(e => e.SettingValue).HasMaxLength(2000);
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SiteSettings_UpdatedAt");
        });

        modelBuilder.Entity<SourceSnapshot>(entity =>
        {
            entity.HasIndex(e => e.RelativePath, "UQ_SourceSnapshots_RelativePath").IsUnique();

            entity.Property(e => e.ContentType).HasMaxLength(150);
            entity.Property(e => e.ImportedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_SourceSnapshots_ImportedAt");
            entity.Property(e => e.Sha256)
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsFixedLength();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
