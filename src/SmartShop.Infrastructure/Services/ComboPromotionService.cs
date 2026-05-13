using SmartShop.Application.Services;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Infrastructure.Services;

public class ComboPromotionService(IComboPromotionRepository repo) : IComboPromotionService
{
    public async Task<ComboMatchResult?> FindApplicableComboAsync(
        Guid storeId,
        IEnumerable<CartItemInput> cartItems,
        CancellationToken ct = default)
    {
        var activeCombos = await repo.GetActiveForStoreAsync(storeId, DateTime.UtcNow, ct);

        if (activeCombos.Count == 0) return null;

        var itemList = cartItems.ToList();

        foreach (var combo in activeCombos)
        {
            // Check if any cart item matches the trigger (ProductId + optional SizeId)
            var matchingItem = itemList.FirstOrDefault(i =>
                i.ProductId == combo.TriggerProductId &&
                (!combo.TriggerSizeId.HasValue || i.SizeId == combo.TriggerSizeId));

            if (matchingItem is null) continue;
            if (matchingItem.Quantity < combo.TriggerMinQuantity) continue;

            // Match found — build result
            return new ComboMatchResult(
                Combo: combo,
                RewardType: combo.RewardType,
                FreeProductId: combo.RewardProductId,
                FreeSizeId: combo.RewardSizeId,
                FreeQuantity: combo.RewardQuantity ?? 0,
                DiscountAmount: combo.RewardAmount ?? 0m
            );
        }

        return null;
    }
}
