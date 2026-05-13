using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.PriceCampaigns.Queries.GetBulkEffectivePrices;

public record BulkEffectivePriceInput(Guid ProductId, Guid? SizeId);

public record BulkEffectivePriceResult(
    Guid ProductId,
    Guid? SizeId,
    decimal BasePrice,
    decimal EffectivePrice,
    bool HasPromotion
);

public record GetBulkEffectivePricesQuery(
    Guid StoreId,
    List<BulkEffectivePriceInput> Items,
    DateTime? At = null
) : IRequest<ApiResponse<List<BulkEffectivePriceResult>>>;
