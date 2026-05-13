using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Services;

public record CartItemInput(Guid ProductId, Guid? SizeId, int Quantity);

public record ComboMatchResult(
    ComboPromotion Combo,
    ComboRewardType RewardType,
    // FreeProduct
    Guid? FreeProductId,
    Guid? FreeSizeId,
    int FreeQuantity,
    // DiscountAmount
    decimal DiscountAmount
);

public interface IComboPromotionService
{
    /// <summary>
    /// Tìm combo đầu tiên thỏa mãn trigger trong cart items.
    /// Return null nếu không có combo nào match.
    /// </summary>
    Task<ComboMatchResult?> FindApplicableComboAsync(
        Guid storeId,
        IEnumerable<CartItemInput> cartItems,
        CancellationToken ct = default);
}
