using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IProductSizeRepository
{
    Task<List<ProductSize>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<ProductSize?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasInventoryAsync(Guid sizeId, CancellationToken ct = default);
    Task AddAsync(ProductSize size, CancellationToken ct = default);
    void Update(ProductSize size);
    void Delete(ProductSize size);
}
