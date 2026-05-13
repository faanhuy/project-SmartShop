using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Products.Queries.GetProducts;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.PriceCampaigns.Queries.GetPriceLists;

public class GetPriceListsQueryHandler(IPriceCampaignRepository repo)
    : IRequestHandler<GetPriceListsQuery, ApiResponse<PagedResult<PriceCampaignSummaryDto>>>
{
    public async Task<ApiResponse<PagedResult<PriceCampaignSummaryDto>>> Handle(
        GetPriceListsQuery request, CancellationToken ct)
    {
        var totalCount = await repo.CountAsync(ct);
        var campaigns = await repo.GetAllAsync(request.Page, request.PageSize, ct);

        var summaries = campaigns.Select(c => new PriceCampaignSummaryDto(
            c.Id, c.Name, c.StartsAt, c.EndsAt, c.AppliesToAll, c.IsActive,
            c.ItemsNav.Count
        )).ToList();

        var paged = new PagedResult<PriceCampaignSummaryDto>(
            summaries, totalCount, request.Page, request.PageSize);

        return ApiResponse<PagedResult<PriceCampaignSummaryDto>>.Ok(paged);
    }
}
