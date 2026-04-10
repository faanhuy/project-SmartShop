using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.AI.Queries.SemanticSearch;

public class SemanticSearchQueryHandler(
    ISemanticKernelService semanticKernel,
    IProductRepository productRepository,
    IAppSettingRepository settings,
    ICacheService cache
) : IRequestHandler<SemanticSearchQuery, IReadOnlyList<SemanticSearchResultDto>>
{
    public async Task<IReadOnlyList<SemanticSearchResultDto>> Handle(
        SemanticSearchQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"ai:search:{request.Query.ToLower().Trim()}:top{request.TopN}";

        // Bước 1: Cache (Redis) + DB products song song — khác service, không conflict
        var cacheTask = cache.GetAsync<List<SemanticSearchResultDto>>(cacheKey, cancellationToken);
        var dbTask    = productRepository.GetPagedAsync(1, int.MaxValue, ct: cancellationToken);
        await Task.WhenAll(cacheTask, dbTask);

        var cached = await cacheTask;
        if (cached is not null) return cached;

        var (allProducts, _) = await dbTask;

        // Bước 2: Settings dùng cùng DbContext → đọc tuần tự sau khi DB xong
        var minScore = await settings.GetDoubleAsync("AI:Search:MinScore", defaultValue: 0.3, cancellationToken);
        var activeProducts   = allProducts.Where(p => p.IsActive).ToList();

        var candidates = activeProducts.Select(p => (p.Id, p.Name, p.Description ?? string.Empty));
        var ranked     = await semanticKernel.SemanticSearchAsync(
            request.Query, candidates, request.TopN, cancellationToken);

        var productById = activeProducts.ToDictionary(p => p.Id);
        var results = ranked
            .Where(r => r.Score >= minScore && productById.ContainsKey(r.Id))
            .Select(r =>
            {
                var p = productById[r.Id];
                return new SemanticSearchResultDto(
                    p.Id, p.Name, p.Description,
                    p.Price, p.OriginalPrice, p.Slug,
                    p.ImageUrl, p.CategoryId, r.Score);
            })
            .ToList();

        _ = cache.SetAsync(cacheKey, results, TimeSpan.FromMinutes(15), cancellationToken);
        return results;
    }
}
