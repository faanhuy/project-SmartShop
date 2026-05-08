using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (!product.IsActive)
            throw new NotFoundException("Product", request.ProductId);

        var cart = await cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = CartEntity.Create(request.UserId);
            cart.AddItem(product.Id, request.Quantity, product.Price);
            await cartRepository.AddAsync(cart, cancellationToken);
        }
        else
        {
            // Check before mutating so we know whether a new CartItem was created
            var isNewItem = !cart.Items.Any(i => i.ProductId == product.Id);

            cart.AddItem(product.Id, request.Quantity, product.Price);

            if (isNewItem)
            {             
                var newItem = cart.Items.First(i => i.ProductId == product.Id);
                await cartRepository.AddCartItemAsync(newItem, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return CartMapper.ToDto(updatedCart!);
    }
}
