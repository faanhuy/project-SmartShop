using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository repository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        var existing = await repository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Slug '{request.Slug}' đã được sử dụng.");

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.CategoryId,
            request.Slug,
            request.ImageUrl,
            request.OriginalPrice,
            request.HasSizes,
            request.SizeType
        );

        await repository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await cache.RemoveByPrefixAsync("products:list:", cancellationToken);

        return new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.OriginalPrice,
            product.Slug, product.ImageUrl, product.IsActive, product.CategoryId, product.CreatedAt,
            product.HasSizes, product.SizeType?.ToString());
    }
}
