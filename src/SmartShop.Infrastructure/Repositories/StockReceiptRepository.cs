using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class StockReceiptRepository(ApplicationDbContext db) : IStockReceiptRepository
{
    public async Task<StockReceipt?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.StockReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<StockReceipt?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
    {
        return await db.StockReceipts
            .Include(r => r.Items)
                .ThenInclude(i => i.Product)
            .Include(r => r.Items)
                .ThenInclude(i => i.Size)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<(List<StockReceipt> Items, int Total)> GetPagedAsync(
        Guid storeId,
        int page,
        int pageSize,
        ReceiptStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.StockReceipts.AsNoTracking().Where(r => r.StoreId == storeId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReceiptDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task AddAsync(StockReceipt receipt, CancellationToken ct = default)
    {
        await db.StockReceipts.AddAsync(receipt, ct);
    }

    public void Update(StockReceipt receipt)
    {
        db.StockReceipts.Update(receipt);
    }

    public void Delete(StockReceipt receipt)
    {
        db.StockReceipts.Remove(receipt);
    }

    public async Task<string> GenerateReceiptNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.Now;
        var datePrefix = today.ToString("yyyyMMdd");

        var count = await db.StockReceipts
            .AsNoTracking()
            .Where(r => r.ReceiptNumber.StartsWith($"SR-{datePrefix}"))
            .CountAsync(ct);

        var seq = (count + 1).ToString("D3");
        return $"SR-{datePrefix}-{seq}";
    }
}
