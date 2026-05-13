using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class StockReceipt : BaseAuditableEntity
{
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public DateTime ReceiptDate { get; private set; }
    public string? Notes { get; private set; }
    public ReceiptStatus Status { get; private set; }

    private readonly List<StockReceiptItem> _items = [];
    public IReadOnlyCollection<StockReceiptItem> Items => _items.AsReadOnly();

    private StockReceipt() { }

    public static StockReceipt Create(Guid storeId, DateTime receiptDate, string? notes, string receiptNumber)
    {
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId không được để trống.", nameof(storeId));
        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new ArgumentException("ReceiptNumber không được để trống.", nameof(receiptNumber));

        return new StockReceipt
        {
            Id = Guid.NewGuid(),
            ReceiptNumber = receiptNumber,
            StoreId = storeId,
            ReceiptDate = receiptDate,
            Notes = notes,
            Status = ReceiptStatus.Pending
        };
    }

    public void AddItem(StockReceiptItem item) => _items.Add(item);

    public void ClearItems() => _items.Clear();

    public void Complete() => Status = ReceiptStatus.Completed;

    public void Cancel() => Status = ReceiptStatus.Cancelled;

    public void UpdateNotes(string? notes) => Notes = notes;

    public void UpdateReceiptDate(DateTime date) => ReceiptDate = date;
}
