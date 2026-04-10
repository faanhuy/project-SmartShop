using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key, CancellationToken ct = default);
    Task<double>      GetDoubleAsync(string key, double defaultValue = 0, CancellationToken ct = default);
    Task<string>      GetStringAsync(string key, string defaultValue = "", CancellationToken ct = default);
    Task<bool>        GetBoolAsync(string key, bool defaultValue = false, CancellationToken ct = default);
    Task              UpsertAsync(AppSetting setting, CancellationToken ct = default);
    Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken ct = default);
}
