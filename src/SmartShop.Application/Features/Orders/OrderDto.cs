namespace SmartShop.Application.Features.Orders;

public class OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
}

public class OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal OriginalAmount { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? CouponCode { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
