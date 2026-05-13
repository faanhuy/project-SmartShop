using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.PriceCampaigns.Queries.GetBulkEffectivePrices;

public class GetBulkEffectivePricesQueryHandler(
    IPriceCampaignRepository priceCampaignRepo,
    IProductRepository productRepo
) : IRequestHandler<GetBulkEffectivePricesQuery, ApiResponse<List<BulkEffectivePriceResult>>>
{
    public async Task<ApiResponse<List<BulkEffectivePriceResult>>> Handle(
        GetBulkEffectivePricesQuery request, CancellationToken ct)
    {
        var at = request.At ?? DateTime.UtcNow;

        // Load base prices — keyed by productId
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var basePrices = new Dictionary<Guid, decimal>();

        foreach (var pid in productIds)
        {
            var product = await productRepo.GetByIdAsync(pid, ct);
            if (product is not null)
                basePrices[pid] = product.Price;
        }

        // Bulk load effective price rules
        var keys = request.Items.Select(i => (i.ProductId, i.SizeId)).ToList();
        var effectiveRules = await priceCampaignRepo.GetEffectivePriceItemsAsync(
            request.StoreId, keys, at, ct);

        var results = request.Items.Select(item =>
        {
            var basePrice = basePrices.GetValueOrDefault(item.ProductId, 0m);
            var key = (item.ProductId, item.SizeId);

            if (effectiveRules.TryGetValue(key, out var rule))
            {
                var effectivePrice = (PriceRuleType)rule.ruleType switch
                {
                    PriceRuleType.Coefficient => basePrice * rule.discountValue,
                    PriceRuleType.FixedPrice => rule.discountValue,
                    _ => basePrice
                };

                return new BulkEffectivePriceResult(
                    item.ProductId, item.SizeId, basePrice, effectivePrice, HasPromotion: true);
            }

            return new BulkEffectivePriceResult(
                item.ProductId, item.SizeId, basePrice, basePrice, HasPromotion: false);
        }).ToList();

        return ApiResponse<List<BulkEffectivePriceResult>>.Ok(results);
    }
}
