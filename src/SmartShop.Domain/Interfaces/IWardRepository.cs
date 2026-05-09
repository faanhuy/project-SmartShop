using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IWardRepository
{
    Task<IEnumerable<Ward>> GetByProvinceAsync(int provinceId);
    Task<Ward?> GetByIdAsync(int id);
}
