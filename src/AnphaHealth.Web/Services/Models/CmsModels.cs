using System.ComponentModel.DataAnnotations;

namespace AnphaHealth.Web.Services.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
}

public sealed record CmsQuery(string? Search = null, string? Kind = null, bool? IsPublished = null,
    int Page = 1, int PageSize = 20, int? CategoryId = null);

public sealed record DashboardStats(int ContentCount, int PublishedCount, int ProductCount,
    int MediaCount, int UnreadCount, IReadOnlyList<ContentListItem> RecentContent);

public sealed record ContentListItem(int Id, string Title, string Slug, string Kind, bool IsPublished,
    DateTime UpdatedAt, string? FeaturedImage);

public sealed class ContentInput
{
    public int? GuideProductId { get; set; }

    [StringLength(120)]
    public string? GuideModel { get; set; }

    [StringLength(450)]
    public string? GuideFilePath { get; set; }

    public bool IsGuideSample { get; set; }

    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tiêu đề."), StringLength(250)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Nhập đường dẫn."), StringLength(450)]
    public string Slug { get; set; } = "";

    [Required, StringLength(30)]
    public string Kind { get; set; } = "Page";

    [StringLength(500)]
    public string Summary { get; set; } = "";

    public string Html { get; set; } = "";
    public bool IsPublished { get; set; }
    public int? FeaturedImageId { get; set; }
    public string? FeaturedImage { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record ProductListItem(int Id, int ContentId, string Name, string Slug, string Brand,
    string Category, string? FeaturedImage, bool IsPublished, bool IsFeatured, int DisplayOrder);

public sealed class ProductInput
{
    public int Id { get; set; }
    public int ContentId { get; set; }

    [Required(ErrorMessage = "Nhập tên sản phẩm."), StringLength(250)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Nhập đường dẫn."), StringLength(450)]
    public string Slug { get; set; } = "";

    [Required(ErrorMessage = "Nhập thương hiệu."), StringLength(150)]
    public string Brand { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục.")]
    public int CategoryId { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = "";

    public string Html { get; set; } = "";
    public int? FeaturedImageId { get; set; }
    public string? FeaturedImage { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed record CategoryItem(int Id, string Name, string Slug, string Icon, int DisplayOrder);

public sealed class CategoryInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nhập tên danh mục."), StringLength(150)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Nhập đường dẫn."), StringLength(120)]
    public string Slug { get; set; } = "";

    public string Icon { get; set; } = "cross";
    public int DisplayOrder { get; set; }
}

public sealed record MediaItem(int Id, string FileName, string LocalPath, string ContentType,
    long ByteLength, string AltText, DateTime CreatedAt);

public sealed class SiteSettingsInput
{
    [Required, StringLength(160)]
    public string SiteName { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(40)]
    public string Phone { get; set; } = "";

    [StringLength(40)]
    public string Fax { get; set; } = "";

    [StringLength(500)]
    public string Address { get; set; } = "";

    [StringLength(500)]
    public string OfficeAddress { get; set; } = "";

    [StringLength(250)]
    public string Tagline { get; set; } = "";
}

public sealed class ContactInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên."), StringLength(120)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email chưa hợp lệ."), StringLength(254)]
    public string Email { get; set; } = "";

    [StringLength(40)]
    public string Phone { get; set; } = "";

    [Required, StringLength(200)]
    public string Interest { get; set; } = "Tư vấn chung";

    [Required(ErrorMessage = "Vui lòng nhập nội dung yêu cầu."), StringLength(4000)]
    public string Message { get; set; } = "";
}

public sealed record InquiryItem(int Id, string Name, string Email, string? Phone,
    string Interest, string Message, string Status, DateTime CreatedAt);

public sealed class CmsException(string message) : Exception(message);
