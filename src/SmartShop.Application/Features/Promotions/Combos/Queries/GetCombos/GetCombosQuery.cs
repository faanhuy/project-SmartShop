using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Promotions.Combos.Queries.GetCombos;

public record GetCombosQuery(int Page = 1, int PageSize = 20) : IRequest<ApiResponse<GetCombosResult>>;

public record GetCombosResult(List<ComboPromotionDto> Items, int TotalCount);
