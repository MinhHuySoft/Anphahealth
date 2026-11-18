using AnphaHealth.Web.Data.Entities;
using AnphaHealth.Web.Data.Repository;
using AnphaHealth.Web.Services.Contracts;
using AnphaHealth.Web.Services.Models;
using Microsoft.EntityFrameworkCore;

namespace AnphaHealth.Web.Services.Implementation;

internal sealed class SiteSettingsService(IRepository<SiteSetting> repository, CmsAccess access) : ISiteSettingsService
{
    public Task<SiteSettingsInput> GetAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(async (rows, token) =>
        {
            var values = await rows.ToDictionaryAsync(item => item.SettingKey, item => item.SettingValue, token);
            string Get(string key) => values.GetValueOrDefault(key) ?? "";
            return new SiteSettingsInput
            {
                SiteName = Get(nameof(SiteSettingsInput.SiteName)),
                Email = Get(nameof(SiteSettingsInput.Email)),
                Phone = Get(nameof(SiteSettingsInput.Phone)),
                Fax = Get(nameof(SiteSettingsInput.Fax)),
                Address = Get(nameof(SiteSettingsInput.Address)),
                OfficeAddress = Get(nameof(SiteSettingsInput.OfficeAddress)),
                Tagline = Get(nameof(SiteSettingsInput.Tagline))
            };
        }, cancellationToken);

    public async Task SaveAsync(SiteSettingsInput input, CancellationToken cancellationToken = default)
    {
        await access.RequireAsync();
        CmsValidation.Validate(input);
        var settings = new Dictionary<string, string>
        {
            [nameof(input.SiteName)] = input.SiteName,
            [nameof(input.Email)] = input.Email,
            [nameof(input.Phone)] = input.Phone,
            [nameof(input.Fax)] = input.Fax,
            [nameof(input.Address)] = input.Address,
            [nameof(input.OfficeAddress)] = input.OfficeAddress,
            [nameof(input.Tagline)] = input.Tagline
        };
        try
        {
            await repository.WriteAsync(async (_, rows, token) =>
            {
                var current = await rows.Where(row => settings.Keys.Contains(row.SettingKey))
                    .ToDictionaryAsync(row => row.SettingKey, token);
                var now = DateTime.UtcNow;
                foreach (var (key, value) in settings)
                {
                    if (!current.TryGetValue(key, out var entity))
                        rows.Add(entity = new SiteSetting { SettingKey = key });
                    entity.SettingValue = value.Trim();
                    entity.UpdatedAt = now;
                }
                return true;
            }, cancellationToken);
        }
        catch (DbUpdateException error) when (CmsValidation.IsUniqueViolation(error))
        {
            throw new CmsException("Cấu hình vừa được lưu ở phiên khác. Hãy tải lại rồi thử lại.");
        }
    }
}
