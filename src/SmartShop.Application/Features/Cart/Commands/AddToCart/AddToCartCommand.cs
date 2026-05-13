using MediatR;

namespace SmartShop.Application.Features.Cart.Commands.AddToCart;

public record AddToCartCommand(Guid UserId, Guid ProductId, int Quantity, Guid? SizeId = null) : IRequest<CartDto>;
