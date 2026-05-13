using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ComboPromotionRepository(ApplicationDbContext db) : IComboPromotionRepository
{
    public async Task<ComboPromotion?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ComboPromotions.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ComboPromotion>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await db.ComboPromotions
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await db.ComboPromotions.CountAsync(ct);

    public async Task<IReadOnlyList<ComboPromotion>> GetActiveForStoreAsync(
        Guid storeId, DateTime at, CancellationToken ct = default)
    {
        var result = await db.ComboPromotions
            .AsNoTracking()
            .Where(c => c.IsActive
                && (c.StartsAt == null || c.StartsAt <= at)
                && (c.EndsAt == null || at < c.EndsAt)
                && (c.StoreId == null || c.StoreId == storeId))
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    public async Task AddAsync(ComboPromotion combo, CancellationToken ct = default)
        => await db.ComboPromotions.AddAsync(combo, ct);

    public void Update(ComboPromotion combo)
        => db.ComboPromotions.Update(combo);

    public void Remove(ComboPromotion combo)
        => db.ComboPromotions.Remove(combo);
}
