using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class StoreSizeInventory : BaseAuditableEntity
{
    public Guid StoreId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid SizeId { get; private set; }
    public int Quantity { get; private set; }
    public int LowStockThreshold { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private StoreSizeInventory() { }

    public static StoreSizeInventory Create(
        Guid storeId, Guid productId, Guid sizeId,
        int quantity = 0, int lowStockThreshold = 5)
    {
        return new StoreSizeInventory
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = productId,
            SizeId = sizeId,
            Quantity = quantity,
            LowStockThreshold = lowStockThreshold
        };
    }

    public void SetQuantity(int quantity) => Quantity = Math.Max(0, quantity);

    public bool DeductStock(int qty)
    {
        if (Quantity < qty) return false;
        Quantity -= qty;
        return true;
    }

    public void RestoreStock(int qty) => Quantity += qty;
}
