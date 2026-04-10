using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface IProductEmbeddingRepository
{
    Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductEmbedding>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetEmbeddedProductIdsAsync(CancellationToken ct = default);
    Task AddAsync(ProductEmbedding embedding, CancellationToken ct = default);
    void Update(ProductEmbedding embedding);
}
