using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class MediaService(IRepository<MediaAsset> repository, IWebHostEnvironment environment,
    IConfiguration configuration, CmsAccess access) : IMediaService
{
    private const int MaxBytes = 8 * 1024 * 1024;

    public static string UploadDirectory(IWebHostEnvironment environment, IConfiguration configuration) =>
        Path.GetFullPath(configuration["Media:UploadDirectory"] ?? "App_Data/uploads", environment.ContentRootPath);

    public async Task<PagedResult<MediaItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await repository.ReadAsync((rows, token) =>
        {
            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                rows = rows.Where(row => row.FileName.Contains(search) || row.AltText.Contains(search));
            if (query.Kind == "Image")
                rows = rows.Where(row => row.ContentType.StartsWith("image/"));
            return CmsValidation.PageAsync(rows.OrderByDescending(row => row.CreatedAt).ThenByDescending(row => row.Id)
                .Select(row => new MediaItem(row.Id, row.FileName, row.LocalPath, row.ContentType, row.ByteLength,
                    row.AltText, row.CreatedAt)), query, token);
        }, cancellationToken);
    }

    public async Task<MediaItem?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await repository.ReadAsync((rows, token) => rows.Where(row => row.Id == id)
            .Select(row => new MediaItem(row.Id, row.FileName, row.LocalPath, row.ContentType, row.ByteLength,
                row.AltText, row.CreatedAt)).SingleOrDefaultAsync(token), cancellationToken);
    }

    public async Task<MediaItem> UploadAsync(Stream stream, string fileName, string contentType, long byteLength,
        string? altText = null, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        if (byteLength is <= 0 or > MaxBytes)
            throw new CmsException("Dung lượng tối đa của ảnh là 8 MB.");

        var alt = ValidateAltText(altText ?? "");
        var header = new byte[12];
        var count = await stream.ReadAtLeastAsync(header, header.Length, false, cancellationToken);
        var (extension, mime) = DetectImage(header.AsSpan(0, count));
        var safeName = Guid.NewGuid().ToString("N") + extension;
        var directory = UploadDirectory(environment, configuration);
        Directory.CreateDirectory(directory);
        var target = Path.Combine(directory, safeName);

        try
        {
            long length = count;
            await using (var file = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 65536, FileOptions.Asynchronous))
            {
                await file.WriteAsync(header.AsMemory(0, count), cancellationToken);
                var buffer = new byte[65536];
                int read;
                while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    length += read;
                    if (length > MaxBytes)
                        throw new CmsException("Dung lượng tối đa của ảnh là 8 MB.");
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (length != byteLength)
                throw new CmsException("Tệp tải lên chưa đầy đủ. Hãy thử lại.");
            var displayName = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = safeName;
            var media = new MediaAsset
            {
                FileName = displayName[..Math.Min(displayName.Length, 260)],
                LocalPath = "/media/uploads/" + safeName,
                ContentType = mime,
                ByteLength = length,
                AltText = alt
            };
            await repository.WriteAsync((_, rows, _) =>
            {
                rows.Add(media);
                return Task.FromResult(media);
            }, cancellationToken);
            return new MediaItem(media.Id, media.FileName, media.LocalPath, media.ContentType,
                media.ByteLength, media.AltText, media.CreatedAt);
        }
        catch
        {
            File.Delete(target);
            throw;
        }
    }

    public async Task UpdateAltTextAsync(int id, string altText, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        var alt = ValidateAltText(altText);
        await repository.WriteAsync(async (_, rows, token) =>
        {
            var entry = await rows.SingleOrDefaultAsync(row => row.Id == id, token)
                ?? throw new CmsException("Tệp không còn tồn tại.");
            entry.AltText = alt;
            return entry.Id;
        }, cancellationToken);
    }

    private static string ValidateAltText(string value)
    {
        if (value.Length > 250)
            throw new CmsException("Mô tả ảnh tối đa 250 ký tự.");
        return value.Trim();
    }

    private static (string Extension, string ContentType) DetectImage(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return (".png", "image/png");
        if (header.StartsWith(new byte[] { 255, 216, 255 }))
            return (".jpg", "image/jpeg");
        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))
            return (".gif", "image/gif");
        if (header.Length >= 12 && header.StartsWith("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
            return (".webp", "image/webp");
        throw new CmsException("Chỉ chấp nhận ảnh PNG, JPEG, GIF và WebP.");
    }
}
