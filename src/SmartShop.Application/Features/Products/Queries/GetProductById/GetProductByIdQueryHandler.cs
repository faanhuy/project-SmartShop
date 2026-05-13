using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(
    IProductRepository repository,
    IPriceCampaignRepository priceCampaignRepository,
    ICacheService cache
) : IRequestHandler<GetProductByIdQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Cache key includes storeId so pricing is per-store
        var cacheKey = $"products:detail:{request.Id}:{request.StoreId}";

        var cached = await cache.GetAsync<ProductDetailDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var product = await repository.GetByIdWithSizesAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        // Build sizes dto
        var sizes = product.Sizes
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new SizeDto(s.Id, s.SizeLabel, s.DisplayOrder, s.IsActive))
            .ToList()
            .AsReadOnly();

        decimal effectivePrice = product.Price;

        // Overlay effective pricing if storeId provided
        if (request.StoreId.HasValue)
        {
            var at = DateTime.UtcNow;

            if (product.HasSizes)
            {
                // Query per-size effective prices — return first matching (use product-level entry if exists)
                var sizeKeys = product.Sizes
                    .Where(s => s.IsActive)
                    .Select(s => (product.Id, (Guid?)s.Id))
                    .ToList();

                // Also include no-size key as fallback
                sizeKeys.Add((product.Id, null));

                var rules = await priceCampaignRepository.GetEffectivePriceItemsAsync(
                    request.StoreId.Value, sizeKeys, at, cancellationToken);

                // Pick best available rule (non-sized first as product-level)
                if (rules.TryGetValue((product.Id, null), out var productRule))
                {
                    effectivePrice = ComputePrice(product.Price, productRule);
                }
                else if (rules.Count > 0)
                {
                    // Use first size rule as representative
                    var firstRule = rules.Values.First();
                    effectivePrice = ComputePrice(product.Price, firstRule);
                }
            }
            else
            {
                var keys = new[] { (product.Id, (Guid?)null) };
                var rules = await priceCampaignRepository.GetEffectivePriceItemsAsync(
                    request.StoreId.Value, keys, at, cancellationToken);

                if (rules.TryGetValue((product.Id, null), out var rule))
                    effectivePrice = ComputePrice(product.Price, rule);
            }
        }

        var dto = new ProductDetailDto(
            product.Id, product.Name, product.Description, product.Price, product.OriginalPrice,
            product.Slug, product.ImageUrl, product.IsActive, product.CategoryId, product.CreatedAt,
            HasSizes: product.HasSizes,
            SizeType: product.SizeType?.ToString(),
            Sizes: sizes,
            EffectivePrice: effectivePrice);

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

        return dto;
    }

    private static decimal ComputePrice(decimal basePrice, (int ruleType, decimal discountValue) rule) =>
        (PriceRuleType)rule.ruleType switch
        {
            PriceRuleType.Coefficient => basePrice * rule.discountValue,
            PriceRuleType.FixedPrice => rule.discountValue,
            _ => basePrice
        };
}
