using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Guid? SizeId { get; private set; }
    public string? SizeLabel { get; private set; }

    public Cart? Cart { get; private set; }
    public Product? Product { get; private set; }

    public decimal SubTotal => UnitPrice * Quantity;

    private CartItem() { }

    public static CartItem Create(
        Guid cartId, Guid productId, int quantity, decimal unitPrice,
        Guid? sizeId = null, string? sizeLabel = null)
    {
        return new CartItem
        {
            CartId = cartId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            SizeId = sizeId,
            SizeLabel = sizeLabel
        };
    }

    public void IncreaseQuantity(int amount) => Quantity += amount;

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Số lượng phải lớn hơn 0.");
        Quantity = quantity;
    }
}
