using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IStoreInventoryRepository
{
    Task<StoreInventory?> GetByStoreAndProductAsync(Guid storeId, Guid productId, CancellationToken ct = default);
    Task<IEnumerable<StoreInventory>> GetByStoreAndProductsAsync(Guid storeId, IEnumerable<Guid> productIds, CancellationToken ct = default);
    Task<IEnumerable<StoreInventory>> GetByStoreIdAsync(Guid storeId, CancellationToken ct = default);
    Task<IEnumerable<StoreInventory>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(StoreInventory inventory, CancellationToken ct = default);
    Task<int> GetTotalStockByProductAsync(Guid productId, CancellationToken ct = default);
}
