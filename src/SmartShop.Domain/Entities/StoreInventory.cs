using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class StoreInventory : BaseAuditableEntity
{
    public Guid StoreId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public byte[]? RowVersion { get; private set; }

    public Store? Store { get; private set; }
    public Product? Product { get; private set; }

    private StoreInventory() { }

    public static StoreInventory Create(Guid storeId, Guid productId, int quantity)
    {
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId không được để trống.", nameof(storeId));
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId không được để trống.", nameof(productId));
        if (quantity < 0)
            throw new ArgumentException("Số lượng tồn kho không được âm.", nameof(quantity));

        return new StoreInventory
        {
            StoreId = storeId,
            ProductId = productId,
            Quantity = quantity,
            LowStockThreshold = 5
        };
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng cần trừ phải lớn hơn 0.", nameof(quantity));
        if (quantity > Quantity)
            throw new InvalidOperationException($"Không đủ tồn kho. Hiện có: {Quantity}, cần: {quantity}.");

        Quantity -= quantity;
    }

    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng cần khôi phục phải lớn hơn 0.", nameof(quantity));

        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Số lượng tồn kho không được âm.", nameof(quantity));

        Quantity = quantity;
    }
}
