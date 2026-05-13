using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ProductSizeRepository(ApplicationDbContext context) : IProductSizeRepository
{
    public async Task<List<ProductSize>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await context.ProductSizes
            .AsNoTracking()
            .Where(ps => ps.ProductId == productId)
            .OrderBy(ps => ps.DisplayOrder)
            .ToListAsync(ct);
    }

    public async Task<ProductSize?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.ProductSizes
            .FirstOrDefaultAsync(ps => ps.Id == id, ct);
    }

    public async Task<bool> HasInventoryAsync(Guid sizeId, CancellationToken ct = default)
    {
        return await context.StoreSizeInventories
            .AsNoTracking()
            .AnyAsync(ssi => ssi.SizeId == sizeId && ssi.Quantity > 0, ct);
    }

    public async Task AddAsync(ProductSize size, CancellationToken ct = default)
    {
        await context.ProductSizes.AddAsync(size, ct);
    }

    public void Update(ProductSize size)
    {
        context.ProductSizes.Update(size);
    }

    public void Delete(ProductSize size)
    {
        context.ProductSizes.Remove(size);
    }
}
