using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Queries.GetProducts;

public class GetProductsQueryHandler(
    IProductRepository repository,
    ICacheService cache
) : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var sortByKey = request.SortBy.ToString().ToLower();
        var cacheKey = $"products:list:p{request.Page}:ps{request.PageSize}:cat{request.CategoryId}:q{request.Search}:s{sortByKey}";

        var cached = await cache.GetAsync<PagedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var sortByStr = request.SortBy switch
        {
            ProductSortBy.PriceAsc    => "price_asc",
            ProductSortBy.PriceDesc   => "price_desc",
            ProductSortBy.NameAsc     => "name_asc",
            ProductSortBy.NameDesc    => "name_desc",
            ProductSortBy.BestSelling => "best_selling",
            _                         => "newest",
        };

        var (items, totalCount) = await repository.GetPagedAsync(
            request.Page, request.PageSize, request.CategoryId, request.Search, sortByStr, cancellationToken);

        var dtos = items.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.OriginalPrice,
            p.Slug, p.ImageUrl, p.IsActive, p.CategoryId, p.CreatedAt,
            p.HasSizes, p.SizeType?.ToString()));

        var result = new PagedResult<ProductDto>(dtos, totalCount, request.Page, request.PageSize);

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }
}
