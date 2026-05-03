using SmartShop.Domain.Common;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Enums;
namespace SmartShop.Domain.Entities;

public class Coupon : BaseAuditableEntity
{
    public DiscountType DiscountType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public decimal DiscountValue { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int MaxUsage { get; private set; }
    public int UsedQuantity { get; private set; }
    public decimal MinOrderValue { get; private set; }
    public string? Description { get; private set; }
    private Coupon() { }

    public static Coupon Create(string code, DiscountType discountType, decimal discountValue, DateTime expiresAt, int maxUsage, string description, decimal minOrderValue)
    {
        return new Coupon
        {
            Code = code,
            DiscountType = discountType,
            DiscountValue = discountValue,
            ExpiresAt = expiresAt,
            MaxUsage = maxUsage,
            MinOrderValue = minOrderValue,
            UsedQuantity = 0,
            Description = description
        };
    }

    public void Update(DiscountType discountType, decimal discountValue, DateTime expiresAt, int maxUsage, string description, decimal minOrderValue)
    {
        DiscountType = discountType;
        DiscountValue = discountValue;
        ExpiresAt = expiresAt;
        MaxUsage = maxUsage;
        MinOrderValue = minOrderValue;
        Description = description;
    }
    public decimal CalculateDiscount(decimal orderTotal)
    {

        if (!MeetsMinOrderValue(orderTotal))
            throw new InvalidOperationException("Giá trị đơn hàng không đạt yêu cầu để sử dụng Mã khuyến mãi");

        if (DiscountType == DiscountType.Percentage)
            return orderTotal * DiscountValue / 100;

        return Math.Min(DiscountValue, orderTotal);
    }
    public int RemainingUsage => MaxUsage - UsedQuantity;

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool HasRemaining() => UsedQuantity < MaxUsage;
    public bool MeetsMinOrderValue(decimal orderTotal) => orderTotal >= MinOrderValue;
    public bool IsValidForOrder(decimal orderTotal) => MeetsMinOrderValue(orderTotal) && !IsExpired() && HasRemaining();
    public void Use()
    {
        if (IsExpired())
            throw new ConflictException($"Coupon '{Code}' đã hết hạn");

        if (!HasRemaining())
            throw new ConflictException($"Coupon '{Code}' đã hết lượt sử dụng");

        UsedQuantity++;
    }

    public void Refund()
    {
        if (UsedQuantity > 0)
            UsedQuantity--;
    }
    private readonly List<CouponUsage> _usages = new List<CouponUsage>();
    public IReadOnlyCollection<CouponUsage> Usages => _usages.AsReadOnly();


}