using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Promotions.Combos.Queries.GetActiveCombos;

public class GetActiveCombosQueryHandler(
    IComboPromotionRepository repo
) : IRequestHandler<GetActiveCombosQuery, ApiResponse<List<ComboPromotionDto>>>
{
    public async Task<ApiResponse<List<ComboPromotionDto>>> Handle(
        GetActiveCombosQuery query, CancellationToken ct)
    {
        var combos = await repo.GetActiveForStoreAsync(query.StoreId, DateTime.UtcNow, ct);

        var dtos = combos
            .Select(c => CreateComboPromotionCommandHandler.MapToDto(c, string.Empty, null, null, null))
            .ToList();

        return ApiResponse<List<ComboPromotionDto>>.Ok(dtos);
    }
}
