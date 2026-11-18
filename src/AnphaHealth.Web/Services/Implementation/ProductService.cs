using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class ProductService(IRepository<Product> products, IRepository<ProductCategory> categories, CmsAccess access)
    : IProductService
{
    public async Task<PagedResult<ProductListItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await products.ReadAsync((rows, token) =>
        {
            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                rows = rows.Where(row => row.Name.Contains(search) || row.Brand.Contains(search)
                    || row.ContentPage.Slug.Contains(search));
            if (query.CategoryId.HasValue)
                rows = rows.Where(row => row.ProductCategoryId == query.CategoryId);
            if (query.IsPublished.HasValue)
                rows = rows.Where(row => row.IsPublished == query.IsPublished);

            return CmsValidation.PageAsync(rows.OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id)
                .Select(row => new ProductListItem(row.Id, row.ContentPageId, row.Name, row.ContentPage.Slug,
                    row.Brand, row.ProductCategory.Name, row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null,
                    row.IsPublished, row.IsFeatured, row.DisplayOrder)), query, token);
        }, cancellationToken);
    }

    public async Task<ProductInput?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await products.ReadAsync((rows, token) => rows.Where(row => row.Id == id).Select(row => new ProductInput
        {
            Id = row.Id,
            ContentId = row.ContentPageId,
            Name = row.Name,
            Slug = row.ContentPage.Slug,
            Brand = row.Brand,
            CategoryId = row.ProductCategoryId,
            Description = row.Description,
            Html = row.ContentPage.Html,
            FeaturedImageId = row.FeaturedImageId,
            FeaturedImage = row.FeaturedImage != null ? row.FeaturedImage.LocalPath : null,
            IsFeatured = row.IsFeatured,
            IsPublished = row.IsPublished,
            DisplayOrder = row.DisplayOrder,
            UpdatedAt = row.UpdatedAt
        }).SingleOrDefaultAsync(token), cancellationToken);
    }

    public async Task<int> SaveAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        CmsValidation.Validate(input);
        var slug = CmsValidation.Slug(input.Slug);

        try
        {
            var product = await products.WriteAsync(async (context, rows, token) =>
            {
                var isNew = input.Id == 0;
                var entity = isNew
                    ? new Product { ContentPage = new ContentPage() }
                    : await rows.Include(row => row.ContentPage).ThenInclude(page => page.ContentPageMedia)
                        .SingleOrDefaultAsync(row => row.Id == input.Id, token)
                        ?? throw new CmsException("Sản phẩm không còn tồn tại.");

                if (!isNew)
                    CmsValidation.CheckVersion(input.UpdatedAt, entity.UpdatedAt);
                if (!await context.ProductCategories.AnyAsync(item => item.Id == input.CategoryId, token))
                    throw new CmsException("Danh mục đã chọn không còn tồn tại.");

                await ContentWriter.ApplyAsync(context, entity.ContentPage, new ContentInput
                {
                    Title = input.Name,
                    Slug = slug,
                    Kind = "Product",
                    Summary = input.Description,
                    Html = input.Html,
                    FeaturedImageId = input.FeaturedImageId,
                    IsPublished = input.IsPublished
                }, slug, token);

                entity.Name = input.Name.Trim();
                entity.Brand = input.Brand.Trim();
                entity.ProductCategoryId = input.CategoryId;
                entity.Description = input.Description.Trim();
                entity.FeaturedImageId = input.FeaturedImageId;
                entity.IsPublished = input.IsPublished;
                entity.IsFeatured = input.IsFeatured;
                entity.DisplayOrder = input.DisplayOrder;
                entity.UpdatedAt = entity.ContentPage.UpdatedAt;
                if (isNew)
                    rows.Add(entity);
                return entity;
            }, cancellationToken);
            return product.Id;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new CmsException("Sản phẩm đã được cập nhật ở phiên khác. Hãy tải lại trước khi lưu.");
        }
        catch (DbUpdateException error) when (CmsValidation.IsUniqueViolation(error))
        {
            throw new CmsException("Đường dẫn này đã được sử dụng.");
        }
    }

    public async Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await categories.ReadAsync((rows, token) => rows.OrderBy(row => row.DisplayOrder).ThenBy(row => row.Id)
            .Select(row => new CategoryItem(row.Id, row.Name, row.Slug, row.Icon, row.DisplayOrder)).ToListAsync(token),
            cancellationToken);
    }

    public async Task<int> SaveCategoryAsync(CategoryInput input, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        CmsValidation.Validate(input);
        var slug = CmsValidation.Slug(input.Slug);
        if (input.Icon is not ("cross" or "heart" or "shield" or "pulse"))
            throw new CmsException("Biểu tượng danh mục không hợp lệ.");

        try
        {
            var category = await categories.WriteAsync(async (_, rows, token) =>
            {
                var entity = input.Id == 0 ? new ProductCategory() :
                    await rows.SingleOrDefaultAsync(row => row.Id == input.Id, token)
                    ?? throw new CmsException("Danh mục không còn tồn tại.");

                entity.Name = input.Name.Trim();
                entity.Slug = slug;
                entity.Icon = input.Icon;
                entity.DisplayOrder = input.DisplayOrder;
                if (input.Id == 0)
                    rows.Add(entity);
                return entity;
            }, cancellationToken);
            return category.Id;
        }
        catch (DbUpdateException error) when (CmsValidation.IsUniqueViolation(error))
        {
            throw new CmsException("Đường dẫn danh mục này đã được sử dụng.");
        }
    }
}
