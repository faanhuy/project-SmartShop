namespace SmartShop.Application.Features.Promotions.Combos;

public record ComboPromotionDto(
    Guid Id,
    string Name,
    Guid TriggerProductId,
    string TriggerProductName,
    Guid? TriggerSizeId,
    string? TriggerSizeLabel,
    int TriggerMinQuantity,
    int RewardType,          // 0=FreeProduct, 1=DiscountAmount
    Guid? RewardProductId,
    string? RewardProductName,
    Guid? RewardSizeId,
    string? RewardSizeLabel,
    int? RewardQuantity,
    decimal? RewardAmount,
    Guid? StoreId,
    DateTime? StartsAt,
    DateTime? EndsAt,
    bool IsActive
);
