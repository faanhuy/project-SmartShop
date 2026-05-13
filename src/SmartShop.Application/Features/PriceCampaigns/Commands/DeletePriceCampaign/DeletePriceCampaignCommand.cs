using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.PriceCampaigns.Commands.DeletePriceCampaign;

public record DeletePriceCampaignCommand(Guid Id) : IRequest<ApiResponse<object?>>;
