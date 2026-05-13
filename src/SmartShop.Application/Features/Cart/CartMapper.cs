using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Features.Cart;

internal static class CartMapper
{
    internal static CartDto ToDto(CartEntity cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = cart.Items.Select(i => new CartItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                SizeId = i.SizeId,
                SizeLabel = i.SizeLabel
            }).ToList(),
            TotalAmount = cart.TotalAmount
        };
    }
}
