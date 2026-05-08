using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        product.Update(request.Name, request.Description, request.Price, request.ImageUrl, request.OriginalPrice);
        repository.Update(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate caches
        await cache.RemoveAsync($"products:id:{request.Id}", cancellationToken);
        await cache.RemoveByPrefixAsync("products:list:", cancellationToken);

        return new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.OriginalPrice,
            product.Slug, product.ImageUrl, product.IsActive, product.CategoryId, product.CreatedAt);
    }
}
