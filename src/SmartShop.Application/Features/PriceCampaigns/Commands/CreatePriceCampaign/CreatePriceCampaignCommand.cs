using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.PriceCampaigns.Commands.CreatePriceCampaign;

public record CreatePriceCampaignItemInput(
    Guid ProductId,
    Guid? SizeId,
    int RuleType,
    decimal DiscountValue
);

public record CreatePriceCampaignCommand(
    string Name,
    DateTime StartsAt,
    DateTime EndsAt,
    bool AppliesToAll,
    List<Guid> StoreIds,
    List<CreatePriceCampaignItemInput> Items
) : IRequest<ApiResponse<PriceCampaignDto>>;
