using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Store>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(Store store, CancellationToken ct = default);
    void Update(Store store);
}
