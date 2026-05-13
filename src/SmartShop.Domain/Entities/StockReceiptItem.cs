using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class StockReceiptItem : BaseAuditableEntity
{
    public Guid StockReceiptId { get; private set; }
    public StockReceipt StockReceipt { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid? SizeId { get; private set; }
    public Size? Size { get; private set; }
    public int Quantity { get; private set; }
    public string? Notes { get; private set; }

    private StockReceiptItem() { }

    public static StockReceiptItem Create(Guid stockReceiptId, Guid productId, Guid? sizeId, int quantity, string? notes = null)
    {
        if (stockReceiptId == Guid.Empty)
            throw new ArgumentException("StockReceiptId không được để trống.", nameof(stockReceiptId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId không được để trống.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(quantity));

        return new StockReceiptItem
        {
            Id = Guid.NewGuid(),
            StockReceiptId = stockReceiptId,
            ProductId = productId,
            SizeId = sizeId,
            Quantity = quantity,
            Notes = notes
        };
    }
}
