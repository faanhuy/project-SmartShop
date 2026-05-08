using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Queries.GetProductBySlug;

public class GetProductBySlugQueryHandler(
    IProductRepository repository,
    ICacheService cache
) : IRequestHandler<GetProductBySlugQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:slug:{request.Slug}";

        var cached = await cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var product = await repository.GetBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Slug);

        var dto = new ProductDto(
            product.Id, product.Name, product.Description, product.Price, product.OriginalPrice,
            product.Slug, product.ImageUrl, product.IsActive, product.CategoryId, product.CreatedAt);

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

        return dto;
    }
}
