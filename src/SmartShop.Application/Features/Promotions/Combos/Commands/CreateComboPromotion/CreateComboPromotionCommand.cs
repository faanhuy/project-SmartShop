using MediatR;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;

public record CreateComboPromotionCommand(
    string Name,
    Guid TriggerProductId,
    Guid? TriggerSizeId,
    int TriggerMinQuantity,
    ComboRewardType RewardType,
    Guid? RewardProductId,
    Guid? RewardSizeId,
    int? RewardQuantity,
    decimal? RewardAmount,
    Guid? StoreId,
    DateTime? StartsAt,
    DateTime? EndsAt
) : IRequest<ComboPromotionDto>;
