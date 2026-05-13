using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class Cart : BaseAuditableEntity
{
    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    private readonly List<CartItem> _items = [];
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.UnitPrice * i.Quantity);

    private Cart() { }

    public static Cart Create(Guid userId)
    {
        return new Cart
        {
            UserId = userId
        };
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice,
        Guid? sizeId = null, string? sizeLabel = null)
    {
        var existing = _items.FirstOrDefault(i =>
            i.ProductId == productId && i.SizeId == sizeId);
        if (existing != null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(CartItem.Create(Id, productId, quantity, unitPrice, sizeId, sizeLabel));
    }

    public void RemoveItem(Guid productId, Guid? sizeId = null)
    {
        var item = _items.FirstOrDefault(i =>
            i.ProductId == productId && i.SizeId == sizeId);
        if (item != null)
            _items.Remove(item);
    }

    public void UpdateItemQuantity(Guid productId, int quantity, Guid? sizeId = null)
    {
        var item = _items.FirstOrDefault(i =>
                i.ProductId == productId && i.SizeId == sizeId)
            ?? throw new InvalidOperationException("Sản phẩm không có trong giỏ hàng.");
        item.UpdateQuantity(quantity);
    }

    public void Clear()
    {
        _items.Clear();
    }
}
