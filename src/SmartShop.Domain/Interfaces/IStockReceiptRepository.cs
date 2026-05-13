using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Interfaces;

public interface IStockReceiptRepository
{
    Task<StockReceipt?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockReceipt?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<(List<StockReceipt> Items, int Total)> GetPagedAsync(Guid storeId, int page, int pageSize, ReceiptStatus? status = null, CancellationToken ct = default);
    Task AddAsync(StockReceipt receipt, CancellationToken ct = default);
    void Update(StockReceipt receipt);
    void Delete(StockReceipt receipt);
    Task<string> GenerateReceiptNumberAsync(CancellationToken ct = default);
}
