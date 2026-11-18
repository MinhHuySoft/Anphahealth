using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AnphaHealth.Web.Data;
using AnphaHealth.Web.Services.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal static partial class CmsValidation
{
    public static void Validate(object input)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
            throw new CmsException(results[0].ErrorMessage ?? "Dữ liệu không hợp lệ.");
    }

    public static string Slug(string value, bool allowHierarchy = false)
    {
        var slug = value.Trim().Trim('/').ToLowerInvariant();
        if (slug.Length > 450 || !SlugPattern().IsMatch(slug) || (!allowHierarchy && slug.Contains('/')))
            throw new CmsException("Đường dẫn chỉ dùng chữ thường không dấu, chữ số và dấu gạch nối.");
        return slug;
    }

    public static async Task<PagedResult<T>> PageAsync<T>(
        IQueryable<T> rows, CmsQuery query, CancellationToken cancellationToken)
    {
        var size = Math.Clamp(query.PageSize, 1, 100);
        var total = await rows.CountAsync(cancellationToken);
        var page = Math.Clamp(query.Page, 1, Math.Max(1, (total + size - 1) / size));
        var items = await rows.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, total, page, size);
    }

    public static async Task CheckImageAsync(
        AnphaHealthDbContext context, int? imageId, CancellationToken cancellationToken)
    {
        if (imageId.HasValue && !await context.MediaAssets.AnyAsync(
                item => item.Id == imageId && item.ContentType.StartsWith("image/"), cancellationToken))
            throw new CmsException("Ảnh đại diện đã chọn không còn khả dụng.");
    }

    public static void CheckVersion(DateTime? submittedAt, DateTime current)
    {
        if (submittedAt != current)
            throw new CmsException("Nội dung đã được cập nhật ở phiên khác. Hãy tải lại trước khi lưu.");
    }

    public static bool IsUniqueViolation(DbUpdateException error) =>
        error.InnerException is SqlException { Number: 2601 or 2627 };

    [GeneratedRegex(@"^(?!$_)(?:[a-z0-9_-]+)(?:/[a-z0-9_-]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
