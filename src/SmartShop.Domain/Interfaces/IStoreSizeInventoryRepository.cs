using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IStoreSizeInventoryRepository
{
    Task<StoreSizeInventory?> GetAsync(Guid storeId, Guid productId, Guid sizeId, CancellationToken ct = default);
    Task<List<StoreSizeInventory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<List<StoreSizeInventory>> GetByStoreAndSizesAsync(Guid storeId, IEnumerable<Guid> sizeIds, CancellationToken ct = default);
    Task AddAsync(StoreSizeInventory inventory, CancellationToken ct = default);
    void Update(StoreSizeInventory inventory);
}
