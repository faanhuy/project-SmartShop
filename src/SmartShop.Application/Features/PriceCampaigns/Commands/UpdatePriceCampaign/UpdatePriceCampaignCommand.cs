using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.PriceCampaigns.Commands.CreatePriceCampaign;

namespace SmartShop.Application.Features.PriceCampaigns.Commands.UpdatePriceCampaign;

public record UpdatePriceCampaignCommand(
    Guid Id,
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    bool AppliesToAll,
    List<Guid> StoreIds,
    List<CreatePriceCampaignItemInput> Items
) : IRequest<ApiResponse<PriceCampaignDto>>;
