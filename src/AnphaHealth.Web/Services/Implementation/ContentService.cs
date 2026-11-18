using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class ContentService(IRepository<ContentPage> repository, CmsAccess access) : IContentService
{
    public async Task<PagedResult<ContentListItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await repository.ReadAsync((rows, token) =>
        {
            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                rows = rows.Where(row => row.Title.Contains(search) || row.Slug.Contains(search));
            if (!string.IsNullOrEmpty(query.Kind))
                rows = rows.Where(row => row.ContentKind == query.Kind);
            if (query.IsPublished.HasValue)
                rows = rows.Where(row => row.IsPublished == query.IsPublished);

            return CmsValidation.PageAsync(rows.OrderByDescending(row => row.UpdatedAt).ThenByDescending(row => row.Id)
                .Select(row => new ContentListItem(row.Id, row.Title, row.Slug, row.ContentKind,
                    row.IsPublished, row.UpdatedAt, row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null)), query, token);
        }, cancellationToken);
    }

    public async Task<ContentInput?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await repository.ReadAsync((rows, token) => rows.Where(row => row.Id == id)
            .Select(row => new ContentInput
            {
                Id = row.Id,
                Title = row.Title,
                Slug = row.Slug,
                Kind = row.ContentKind,
                Summary = row.Summary,
                Html = row.Html,
                IsPublished = row.IsPublished,
                FeaturedImageId = row.FeaturedImageId,
                FeaturedImage = row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null,
                UpdatedAt = row.UpdatedAt
            }).SingleOrDefaultAsync(token), cancellationToken);
    }

    public async Task<int> SaveAsync(ContentInput input, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        CmsValidation.Validate(input);
        if (input.Kind is not ("Page" or "Article" or "Archive"))
            throw new CmsException("Chỉnh sửa nội dung sản phẩm trong mục Sản phẩm.");

        try
        {
            var page = await repository.WriteAsync(async (context, rows, token) =>
            {
                var isNew = input.Id == 0;
                var entity = isNew ? new ContentPage() : await rows.Include(row => row.ContentPageMedia)
                    .SingleOrDefaultAsync(row => row.Id == input.Id, token)
                    ?? throw new CmsException("Nội dung không còn tồn tại.");

                if (!isNew)
                {
                    if (entity.ContentKind == "Product")
                        throw new CmsException("Chỉnh sửa nội dung sản phẩm trong mục Sản phẩm.");
                    CmsValidation.CheckVersion(input.UpdatedAt, entity.UpdatedAt);
                    if (PublicPageRoutes.IsSystemPage(entity.Slug) &&
                        (entity.Slug != input.Slug || entity.ContentKind != input.Kind))
                        throw new CmsException("Trang hệ thống có đường dẫn và loại nội dung cố định.");
                }

                // Keep legacy hierarchical URLs when editing imported content.
                var slug = !isNew && entity.Slug == input.Slug
                    ? entity.Slug
                    : CmsValidation.Slug(input.Slug, allowHierarchy: input.Kind != "Article");
                await ContentWriter.ApplyAsync(context, entity, input, slug, token);
                if (isNew)
                    rows.Add(entity);
                return entity;
            }, cancellationToken);
            return page.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CmsException("Nội dung đã được cập nhật ở phiên khác. Hãy tải lại trước khi lưu.");
        }
        catch (DbUpdateException error) when (CmsValidation.IsUniqueViolation(error))
        {
            throw new CmsException("Đường dẫn này đã được sử dụng.");
        }
    }
}
