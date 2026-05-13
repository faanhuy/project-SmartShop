using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.PriceCampaigns.Queries.GetPriceCampaignById;

public record GetPriceCampaignByIdQuery(Guid Id) : IRequest<ApiResponse<PriceCampaignDto>>;
