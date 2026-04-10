using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ProductEmbeddingRepository(ApplicationDbContext context) : IProductEmbeddingRepository
{
    public async Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await context.ProductEmbeddings
            .FirstOrDefaultAsync(e => e.ProductId == productId, ct);

    public async Task<IReadOnlyList<ProductEmbedding>> GetAllAsync(CancellationToken ct = default)
        => await context.ProductEmbeddings.ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetEmbeddedProductIdsAsync(CancellationToken ct = default)
        => await context.ProductEmbeddings
            .Select(e => e.ProductId)
            .ToListAsync(ct);

    public async Task AddAsync(ProductEmbedding embedding, CancellationToken ct = default)
        => await context.ProductEmbeddings.AddAsync(embedding, ct);

    public void Update(ProductEmbedding embedding)
        => context.ProductEmbeddings.Update(embedding);
}
