using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Guid? SizeId { get; private set; }
    public string? SizeLabel { get; private set; }
    public decimal? OriginalUnitPrice { get; private set; }
    public Guid? PromotionalPriceId { get; private set; }

    public Order? Order { get; private set; }
    public Product? Product { get; private set; }

    public decimal SubTotal => UnitPrice * Quantity;

    private OrderItem() { }

    public static OrderItem Create(
        Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice,
        Guid? sizeId = null, string? sizeLabel = null,
        decimal? originalUnitPrice = null, Guid? promotionalPriceId = null)
    {
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            SizeId = sizeId,
            SizeLabel = sizeLabel,
            OriginalUnitPrice = originalUnitPrice,
            PromotionalPriceId = promotionalPriceId
        };
    }
}
