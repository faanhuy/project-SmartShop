using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Promotions.Combos.Queries.GetCombos;

public class GetCombosQueryHandler(
    IComboPromotionRepository repo
) : IRequestHandler<GetCombosQuery, ApiResponse<GetCombosResult>>
{
    public async Task<ApiResponse<GetCombosResult>> Handle(GetCombosQuery query, CancellationToken ct)
    {
        var combos = await repo.GetAllAsync(query.Page, query.PageSize, ct);
        var total = await repo.CountAsync(ct);

        var dtos = combos
            .Select(c => CreateComboPromotionCommandHandler.MapToDto(c, string.Empty, null, null, null))
            .ToList();

        return ApiResponse<GetCombosResult>.Ok(new GetCombosResult(dtos, total));
    }
}
