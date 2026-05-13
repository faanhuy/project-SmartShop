using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Features.Cart.Commands.AddToCart;

public class AddToCartCommandHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    IProductSizeRepository productSizeRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<AddToCartCommand, CartDto>
{
    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (!product.IsActive)
            throw new NotFoundException("Product", request.ProductId);

        // Validate size
        Guid? sizeId = null;
        string? sizeLabel = null;

        if (product.HasSizes)
        {
            if (!request.SizeId.HasValue)
                throw new ArgumentException("Sản phẩm này yêu cầu chọn size.");

            var productSize = await productSizeRepository.GetByIdAsync(request.SizeId.Value, cancellationToken)
                ?? throw new NotFoundException("ProductSize", request.SizeId.Value);

            if (!productSize.IsActive)
                throw new ConflictException("Size đã chọn không còn khả dụng.");

            if (productSize.ProductId != product.Id)
                throw new ConflictException("Size không thuộc sản phẩm này.");

            sizeId = productSize.Id;
            sizeLabel = productSize.SizeLabel;
        }
        // If product.HasSizes == false, SizeId is ignored (lenient)

        var cart = await cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = CartEntity.Create(request.UserId);
            cart.AddItem(product.Id, request.Quantity, product.Price, sizeId, sizeLabel);
            await cartRepository.AddAsync(cart, cancellationToken);
        }
        else
        {
            // Uniqueness by (ProductId, SizeId)
            var isNewItem = !cart.Items.Any(i => i.ProductId == product.Id && i.SizeId == sizeId);

            cart.AddItem(product.Id, request.Quantity, product.Price, sizeId, sizeLabel);

            if (isNewItem)
            {
                var newItem = cart.Items.First(i => i.ProductId == product.Id && i.SizeId == sizeId);
                await cartRepository.AddCartItemAsync(newItem, cancellationToken);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedCart = await cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return CartMapper.ToDto(updatedCart!);
    }
}
