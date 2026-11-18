using SlugText = System.Globalization.CharUnicodeInfo;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AnphaHealth.Web.Components.Admin;

internal static partial class AdminLabels
{
    public static string ContentKind(string kind) => kind switch
    {
        "Page" => "Trang",
        "Article" => "Bài viết",
        "Product" => "Sản phẩm",
        "Archive" => "Bản lưu",
        _ => kind
    };

    public static string InquiryStatus(string status) => status switch
    {
        "Unread" => "Chưa đọc",
        "Read" => "Đã đọc",
        "Resolved" => "Đã xử lý",
        _ => status
    };

    public static string FileSize(long bytes) => bytes >= 1024 * 1024
        ? $"{bytes / (1024d * 1024):0.0} MB"
        : $"{Math.Max(1, (int)Math.Ceiling(bytes / 1024d))} KB";

    public static string ToSlug(string input)
    {
        var text = input.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var noMarks = string.Concat(text.Where(c => SlugText.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
        return SlugSeparators().Replace(noMarks.ToLowerInvariant(), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugSeparators();
}
