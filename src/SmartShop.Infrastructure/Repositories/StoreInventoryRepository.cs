using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class StoreInventoryRepository(ApplicationDbContext context) : IStoreInventoryRepository
{
    public async Task<StoreInventory?> GetByStoreAndProductAsync(
        Guid storeId, Guid productId, CancellationToken ct = default)
    {
        return await context.StoreInventories
            .FirstOrDefaultAsync(si => si.StoreId == storeId && si.ProductId == productId, ct);
    }

    public async Task<IEnumerable<StoreInventory>> GetByStoreAndProductsAsync(
        Guid storeId, IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.ToList();
        return await context.StoreInventories
            .Where(si => si.StoreId == storeId && ids.Contains(si.ProductId))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StoreInventory>> GetByStoreIdAsync(
        Guid storeId, CancellationToken ct = default)
    {
        return await context.StoreInventories
            .AsNoTracking()
            .Include(si => si.Product)
            .Where(si => si.StoreId == storeId)
            .OrderBy(si => si.Product!.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<StoreInventory>> GetByProductIdAsync(
        Guid productId, CancellationToken ct = default)
    {
        return await context.StoreInventories
            .AsNoTracking()
            .Include(si => si.Store)
            .Where(si => si.ProductId == productId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(StoreInventory inventory, CancellationToken ct = default)
    {
        await context.StoreInventories.AddAsync(inventory, ct);
    }

    public void Update(StoreInventory inventory)
    {
        context.StoreInventories.Update(inventory);
    }

    public async Task<StoreInventory?> GetAsync(Guid storeId, Guid productId, CancellationToken ct = default)
    {
        return await GetByStoreAndProductAsync(storeId, productId, ct);
    }

    public async Task<int> GetTotalStockByProductAsync(Guid productId, CancellationToken ct = default)
    {
        return await context.StoreInventories
            .AsNoTracking()
            .Where(si => si.ProductId == productId)
            .SumAsync(si => si.Quantity, ct);
    }
}
