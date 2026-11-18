using AnphaHealth.Web.Services.Models;

namespace AnphaHealth.Web.Services.Contracts;

public interface IEditorService<TListItem, TInput>
{
    Task<PagedResult<TListItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default);
    Task<TInput?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(TInput input, CancellationToken cancellationToken = default);
}

public interface IContentService : IEditorService<ContentListItem, ContentInput>;

public interface IProductService : IEditorService<ProductListItem, ProductInput>
{
    Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveCategoryAsync(CategoryInput input, CancellationToken cancellationToken = default);
}

public interface IMediaService
{
    Task<PagedResult<MediaItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default);
    Task<MediaItem?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<MediaItem> UploadAsync(Stream stream, string fileName, string contentType, long byteLength,
        string? altText = null, CancellationToken cancellationToken = default);
    Task UpdateAltTextAsync(int id, string altText, CancellationToken cancellationToken = default);
}

public interface ISiteSettingsService
{
    Task<SiteSettingsInput> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(SiteSettingsInput input, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<DashboardStats> GetAsync(CancellationToken cancellationToken = default);
}

public interface IInquiryService
{
    Task<int> CreateAsync(ContactInput input, CancellationToken cancellationToken = default);
    Task<PagedResult<InquiryItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int id, string status, CancellationToken cancellationToken = default);
}
