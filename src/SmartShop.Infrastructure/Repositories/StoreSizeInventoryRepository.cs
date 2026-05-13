using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class StoreSizeInventoryRepository(ApplicationDbContext context) : IStoreSizeInventoryRepository
{
    public async Task<StoreSizeInventory?> GetAsync(
        Guid storeId, Guid productId, Guid sizeId, CancellationToken ct = default)
    {
        return await context.StoreSizeInventories
            .FirstOrDefaultAsync(ssi =>
                ssi.StoreId == storeId &&
                ssi.ProductId == productId &&
                ssi.SizeId == sizeId, ct);
    }

    public async Task<List<StoreSizeInventory>> GetByProductIdAsync(
        Guid productId, CancellationToken ct = default)
    {
        return await context.StoreSizeInventories
            .AsNoTracking()
            .Where(ssi => ssi.ProductId == productId)
            .ToListAsync(ct);
    }

    public async Task<List<StoreSizeInventory>> GetByStoreAndSizesAsync(
        Guid storeId, IEnumerable<Guid> sizeIds, CancellationToken ct = default)
    {
        var ids = sizeIds.ToList();
        return await context.StoreSizeInventories
            .Where(ssi => ssi.StoreId == storeId && ids.Contains(ssi.SizeId))
            .ToListAsync(ct);
    }

    public async Task AddAsync(StoreSizeInventory inventory, CancellationToken ct = default)
    {
        await context.StoreSizeInventories.AddAsync(inventory, ct);
    }

    public void Update(StoreSizeInventory inventory)
    {
        context.StoreSizeInventories.Update(inventory);
    }
}
