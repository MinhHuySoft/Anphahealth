using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class InquiryService(IRepository<ContactRequest> repository, CmsAccess access) : IInquiryService
{
    public async Task<int> CreateAsync(ContactInput input, CancellationToken cancellationToken = default)
    {
        CmsValidation.Validate(input);
        var request = new ContactRequest
        {
            Name = input.Name.Trim(),
            Email = input.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
            Interest = input.Interest.Trim(),
            Message = input.Message.Trim(),
            Status = "Unread"
        };
        await repository.WriteAsync((_, rows, _) =>
        {
            rows.Add(request);
            return Task.FromResult(request);
        }, cancellationToken);
        return request.Id;
    }

    public async Task<PagedResult<InquiryItem>> ListAsync(CmsQuery query, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        return await repository.ReadAsync((rows, token) =>
        {
            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                rows = rows.Where(row => row.Name.Contains(search) || row.Email.Contains(search)
                    || row.Interest.Contains(search) || row.Message.Contains(search));
            if (!string.IsNullOrEmpty(query.Kind))
                rows = rows.Where(row => row.Status == query.Kind);
            return CmsValidation.PageAsync(rows.OrderByDescending(row => row.CreatedAt).ThenByDescending(row => row.Id)
                .Select(row => new InquiryItem(row.Id, row.Name, row.Email, row.Phone, row.Interest,
                    row.Message, row.Status, row.CreatedAt)), query, token);
        }, cancellationToken);
    }

    public async Task SetStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        if (status is not ("Unread" or "Read" or "Resolved"))
            throw new CmsException("Trạng thái yêu cầu không hợp lệ.");
        await repository.WriteAsync(async (_, rows, token) =>
        {
            var entry = await rows.SingleOrDefaultAsync(row => row.Id == id, token)
                ?? throw new CmsException("Yêu cầu không còn tồn tại.");
            entry.Status = status;
            return entry.Id;
        }, cancellationToken);
    }
}
