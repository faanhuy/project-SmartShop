using Microsoft.EntityFrameworkCore;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class AppSettingRepository(ApplicationDbContext db, ICacheService cache) : IAppSettingRepository
{
    // Cache value string (không cache entity vì private constructor không deserialize được)
    private static string CacheKey(string key) => $"appsetting:v:{key}";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private async Task<string?> GetValueAsync(string key, CancellationToken ct)
    {
        var cached = await cache.GetAsync<string>(CacheKey(key), ct);
        if (cached is not null) return cached;

        var setting = await db.AppSettings.FindAsync([key], ct);
        if (setting is not null)
            await cache.SetAsync(CacheKey(key), setting.Value, Ttl, ct);

        return setting?.Value;
    }

    public async Task<AppSetting?> GetAsync(string key, CancellationToken ct = default)
        => await db.AppSettings.FindAsync([key], ct);

    public async Task<double> GetDoubleAsync(string key, double defaultValue = 0, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);
        return value is not null && double.TryParse(
            value,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v) ? v : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default)
        => await GetValueAsync(key, ct) ?? defaultValue;

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default)
    {
        var value = await GetValueAsync(key, ct);
        return value is not null && bool.TryParse(value, out var v) ? v : defaultValue;
    }

    public async Task UpsertAsync(AppSetting setting, CancellationToken ct = default)
    {
        var existing = await db.AppSettings.FindAsync([setting.Key], ct);
        if (existing is null)
            db.AppSettings.Add(setting);
        else
            existing.SetValue(setting.Value);

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKey(setting.Key), ct);
    }

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken ct = default)
        => await db.AppSettings.OrderBy(x => x.Key).ToListAsync(ct);
}
