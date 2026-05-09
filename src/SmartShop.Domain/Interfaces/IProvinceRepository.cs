using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IProvinceRepository
{
    Task<IEnumerable<Province>> GetAllAsync();
    Task<Province?> GetByIdAsync(int id);
}
