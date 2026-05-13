using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class ComboPromotion : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public Guid TriggerProductId { get; private set; }
    public Guid? TriggerSizeId { get; private set; }
    public int TriggerMinQuantity { get; private set; }
    public ComboRewardType RewardType { get; private set; }

    // FreeProduct fields
    public Guid? RewardProductId { get; private set; }
    public Guid? RewardSizeId { get; private set; }
    public int? RewardQuantity { get; private set; }

    // DiscountAmount field
    public decimal? RewardAmount { get; private set; }

    // Scope
    public Guid? StoreId { get; private set; } // null = all stores
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public bool IsActive { get; private set; }

    private ComboPromotion() { }

    public static ComboPromotion Create(
        string name,
        Guid triggerProductId,
        Guid? triggerSizeId,
        int triggerMinQty,
        ComboRewardType rewardType,
        Guid? rewardProductId,
        Guid? rewardSizeId,
        int? rewardQty,
        decimal? rewardAmount,
        Guid? storeId,
        DateTime? startsAt,
        DateTime? endsAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (triggerMinQty < 1)
            throw new ArgumentException("TriggerMinQuantity phải >= 1.", nameof(triggerMinQty));

        if (rewardType == ComboRewardType.FreeProduct)
        {
            if (!rewardProductId.HasValue || rewardProductId.Value == Guid.Empty)
                throw new ArgumentException("RewardProductId là bắt buộc cho loại FreeProduct.", nameof(rewardProductId));
            if (!rewardQty.HasValue || rewardQty.Value <= 0)
                throw new ArgumentException("RewardQuantity phải > 0 cho loại FreeProduct.", nameof(rewardQty));
        }

        if (rewardType == ComboRewardType.DiscountAmount)
        {
            if (!rewardAmount.HasValue || rewardAmount.Value <= 0)
                throw new ArgumentException("RewardAmount phải > 0 cho loại DiscountAmount.", nameof(rewardAmount));
        }

        return new ComboPromotion
        {
            Name = name.Trim(),
            TriggerProductId = triggerProductId,
            TriggerSizeId = triggerSizeId,
            TriggerMinQuantity = triggerMinQty,
            RewardType = rewardType,
            RewardProductId = rewardProductId,
            RewardSizeId = rewardSizeId,
            RewardQuantity = rewardQty,
            RewardAmount = rewardAmount,
            StoreId = storeId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            IsActive = true
        };
    }

    public void Update(
        string name,
        Guid triggerProductId,
        Guid? triggerSizeId,
        int triggerMinQty,
        ComboRewardType rewardType,
        Guid? rewardProductId,
        Guid? rewardSizeId,
        int? rewardQty,
        decimal? rewardAmount,
        Guid? storeId,
        DateTime? startsAt,
        DateTime? endsAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (triggerMinQty < 1)
            throw new ArgumentException("TriggerMinQuantity phải >= 1.", nameof(triggerMinQty));

        if (rewardType == ComboRewardType.FreeProduct)
        {
            if (!rewardProductId.HasValue || rewardProductId.Value == Guid.Empty)
                throw new ArgumentException("RewardProductId là bắt buộc cho loại FreeProduct.", nameof(rewardProductId));
            if (!rewardQty.HasValue || rewardQty.Value <= 0)
                throw new ArgumentException("RewardQuantity phải > 0 cho loại FreeProduct.", nameof(rewardQty));
        }

        if (rewardType == ComboRewardType.DiscountAmount)
        {
            if (!rewardAmount.HasValue || rewardAmount.Value <= 0)
                throw new ArgumentException("RewardAmount phải > 0 cho loại DiscountAmount.", nameof(rewardAmount));
        }

        Name = name.Trim();
        TriggerProductId = triggerProductId;
        TriggerSizeId = triggerSizeId;
        TriggerMinQuantity = triggerMinQty;
        RewardType = rewardType;
        RewardProductId = rewardProductId;
        RewardSizeId = rewardSizeId;
        RewardQuantity = rewardQty;
        RewardAmount = rewardAmount;
        StoreId = storeId;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void Deactivate() => IsActive = false;

    public bool IsEffectiveAt(DateTime at) =>
        IsActive &&
        (StartsAt == null || StartsAt <= at) &&
        (EndsAt == null || at < EndsAt);

    public bool AppliesToStore(Guid storeId) =>
        StoreId == null || StoreId == storeId;
}
