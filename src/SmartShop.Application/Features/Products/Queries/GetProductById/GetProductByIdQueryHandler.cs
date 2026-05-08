using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(
    IProductRepository repository,
    ICacheService cache
) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:id:{request.Id}";

        var cached = await cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        var dto = new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.OriginalPrice,
            product.Slug, product.ImageUrl, product.IsActive, product.CategoryId, product.CreatedAt);

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

        return dto;
    }
}
