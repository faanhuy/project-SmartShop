using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Promotions.Combos.Queries.GetActiveCombos;

public record GetActiveCombosQuery(Guid StoreId) : IRequest<ApiResponse<List<ComboPromotionDto>>>;
