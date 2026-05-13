using MediatR;

namespace SmartShop.Application.Features.Cart.Commands.RemoveFromCart;

public record RemoveFromCartCommand(Guid UserId, Guid ProductId, Guid? SizeId = null) : IRequest<CartDto>;
