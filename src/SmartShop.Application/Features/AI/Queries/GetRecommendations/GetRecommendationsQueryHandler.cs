using MediatR;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.AI.Queries.GetRecommendations;

public class GetRecommendationsQueryHandler(
    ISemanticKernelService semanticKernel,
    IProductRepository productRepository,
    IAppSettingRepository settings,
    ICacheService cache,
    ILogger<GetRecommendationsQueryHandler> logger
) : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(
        GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"ai:rec:{request.ProductId}:n{request.Count}";

        // Bước 1: Cache (Redis) + DB products song song
        var cacheTask = cache.GetAsync<List<ProductDto>>(cacheKey, cancellationToken);
        var dbTask    = productRepository.GetPagedAsync(1, int.MaxValue, ct: cancellationToken);
        await Task.WhenAll(cacheTask, dbTask);

        var cached = await cacheTask;
        if (cached is not null) return cached;

        var (allProducts, _) = await dbTask;

        // Bước 2: Settings tuần tự sau khi DB xong
        var minScore = await settings.GetDoubleAsync("AI:Recommendations:MinScore", defaultValue: 0.4, cancellationToken);
        var productById      = allProducts.ToDictionary(p => p.Id);

        if (!productById.TryGetValue(request.ProductId, out var sourceProduct))
            throw new NotFoundException("Product", request.ProductId);

        List<ProductDto> results;

        try
        {
            // ── AI path ────────────────────────────────────────────────────
            var candidates = allProducts
                .Where(p => p.IsActive && p.Id != request.ProductId)
                .Select(p => (p.Id, p.Name, p.Description ?? string.Empty));

            var source = (sourceProduct.Id, sourceProduct.Name, sourceProduct.Description ?? string.Empty);
            var ranked = await semanticKernel.GetRecommendationsAsync(source, candidates, request.Count, cancellationToken);

            results = ranked
                .Where(r => r.Score >= minScore && productById.TryGetValue(r.Id, out var p) && p.IsActive)
                .Select(r =>
                {
                    var p = productById[r.Id];
                    return new ProductDto(p.Id, p.Name, p.Description, p.Price,
                        p.OriginalPrice, p.Stock, p.Slug, p.ImageUrl, p.IsActive, p.CategoryId, p.CreatedAt);
                })
                .ToList();

            // Cache 30 phút cho AI results
            _ = cache.SetAsync(cacheKey, results, TimeSpan.FromMinutes(30), cancellationToken);
        }
        catch (ServiceUnavailableException ex)
        {
            // ── Same-category fallback ─────────────────────────────────────
            logger.LogWarning("AI unavailable ({Msg}), falling back to same-category for product {Id}.",
                ex.Message, request.ProductId);

            var (fallbackItems, _) = await productRepository.GetPagedAsync(
                1, request.Count + 1,
                categoryId: sourceProduct.CategoryId,
                ct: cancellationToken);

            results = fallbackItems
                .Where(p => p.IsActive && p.Id != request.ProductId)
                .Take(request.Count)
                .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price,
                    p.OriginalPrice, p.Stock, p.Slug, p.ImageUrl, p.IsActive, p.CategoryId, p.CreatedAt))
                .ToList();

            // Cache ngắn hơn để sớm thử lại AI
            _ = cache.SetAsync(cacheKey, results, TimeSpan.FromMinutes(5), cancellationToken);
        }

        return results;
    }
}
