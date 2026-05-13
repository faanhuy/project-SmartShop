using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.PriceCampaigns.Queries.GetPriceCampaignById;

public class GetPriceCampaignByIdQueryHandler(
    IPriceCampaignRepository repo,
    IProductRepository productRepo,
    IProductSizeRepository sizeRepo
) : IRequestHandler<GetPriceCampaignByIdQuery, ApiResponse<PriceCampaignDto>>
{
    public async Task<ApiResponse<PriceCampaignDto>> Handle(
        GetPriceCampaignByIdQuery request, CancellationToken ct)
    {
        var campaign = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(PriceCampaign), request.Id);

        // Enrich items with product names and size labels
        var productIds = campaign.ItemsNav.Select(i => i.ProductId).Distinct().ToList();
        var productMap = new Dictionary<Guid, string>();
        var sizeMap = new Dictionary<Guid, string>();

        foreach (var pid in productIds)
        {
            var product = await productRepo.GetByIdAsync(pid, ct);
            if (product is not null)
                productMap[pid] = product.Name;
        }

        var sizeIds = campaign.ItemsNav
            .Where(i => i.SizeId.HasValue)
            .Select(i => i.SizeId!.Value)
            .Distinct()
            .ToList();

        foreach (var sid in sizeIds)
        {
            var size = await sizeRepo.GetByIdAsync(sid, ct);
            if (size is not null)
                sizeMap[sid] = size.SizeLabel;
        }

        var itemDtos = campaign.ItemsNav.Select(i => new PriceCampaignItemDto(
            i.Id,
            i.ProductId,
            productMap.GetValueOrDefault(i.ProductId, string.Empty),
            i.SizeId,
            i.SizeId.HasValue ? sizeMap.GetValueOrDefault(i.SizeId.Value) : null,
            (int)i.RuleType,
            i.DiscountValue
        )).ToList();

        var dto = new PriceCampaignDto(
            campaign.Id, campaign.Name, campaign.StartsAt, campaign.EndsAt,
            campaign.AppliesToAll, campaign.IsActive,
            campaign.StoreIds.ToList(),
            itemDtos
        );

        return ApiResponse<PriceCampaignDto>.Ok(dto);
    }
}
