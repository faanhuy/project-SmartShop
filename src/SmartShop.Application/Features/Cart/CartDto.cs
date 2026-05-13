namespace SmartShop.Application.Features.Cart;

public class CartItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
    public Guid? SizeId { get; init; }
    public string? SizeLabel { get; init; }
}

public class CartDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<CartItemDto> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
}
