using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class StoreRepository(ApplicationDbContext context) : IStoreRepository
{
    public async Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Stores
            .Include(s => s.Province)
            .Include(s => s.Ward)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IEnumerable<Store>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await context.Stores
            .AsNoTracking()
            .Include(s => s.Province)
            .Include(s => s.Ward)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Store store, CancellationToken ct = default)
    {
        await context.Stores.AddAsync(store, ct);
    }

    public void Update(Store store)
    {
        context.Stores.Update(store);
    }
}
