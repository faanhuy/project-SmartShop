using MediatR;

namespace SmartShop.Application.Features.Cart.Commands.UpdateCartItem;

public record UpdateCartItemCommand(Guid UserId, Guid ProductId, int Quantity, Guid? SizeId = null) : IRequest<CartDto>;
