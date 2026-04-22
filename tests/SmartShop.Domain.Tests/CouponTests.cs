using FluentAssertions;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using Xunit;

namespace SmartShop.Domain.Tests;

public class CouponTests
{
    private static Coupon Make(
        DiscountType type = DiscountType.Percentage,
        decimal value = 10,
        decimal minOrderValue = 100,
        int maxUsage = 5,
        int daysUntilExpiry = 7) =>
        Coupon.Create("CODE", type, value, DateTime.UtcNow.AddDays(daysUntilExpiry), maxUsage, "desc", minOrderValue);

    // ── CalculateDiscount ─────────────────────────────────────────────────────

    [Fact]
    public void CalculateDiscount_Percentage_ReturnsCorrectAmount()
    {
        var coupon = Make(DiscountType.Percentage, value: 20, minOrderValue: 100);

        var discount = coupon.CalculateDiscount(500m);

        discount.Should().Be(100m); // 20% of 500
    }

    [Fact]
    public void CalculateDiscount_FixedAmount_ReturnsFixedValue()
    {
        var coupon = Make(DiscountType.FixedAmount, value: 50, minOrderValue: 100);

        var discount = coupon.CalculateDiscount(300m);

        discount.Should().Be(50m);
    }

    [Fact]
    public void CalculateDiscount_FixedAmountExceedsTotal_CapsAtOrderTotal()
    {
        var coupon = Make(DiscountType.FixedAmount, value: 500, minOrderValue: 100);

        var discount = coupon.CalculateDiscount(200m);

        discount.Should().Be(200m);
    }

    [Fact]
    public void CalculateDiscount_OrderBelowMinValue_ThrowsInvalidOperationException()
    {
        var coupon = Make(minOrderValue: 200);

        var act = () => coupon.CalculateDiscount(100m);

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Use ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Use_ValidCoupon_IncrementsUsedQuantity()
    {
        var coupon = Make(maxUsage: 5);

        coupon.Use();

        coupon.UsedQuantity.Should().Be(1);
        coupon.RemainingUsage.Should().Be(4);
    }

    [Fact]
    public void Use_MultipleTimes_AccumulatesCount()
    {
        var coupon = Make(maxUsage: 3);

        coupon.Use();
        coupon.Use();

        coupon.UsedQuantity.Should().Be(2);
        coupon.RemainingUsage.Should().Be(1);
    }

    [Fact]
    public void Use_ExpiredCoupon_ThrowsConflictException()
    {
        var coupon = Make(daysUntilExpiry: -1);

        var act = () => coupon.Use();

        act.Should().Throw<ConflictException>().WithMessage("*hết hạn*");
    }

    [Fact]
    public void Use_NoRemainingUsage_ThrowsConflictException()
    {
        var coupon = Make(maxUsage: 1);
        coupon.Use(); // exhaust it

        var act = () => coupon.Use();

        act.Should().Throw<ConflictException>().WithMessage("*hết lượt*");
    }

    // ── Refund ────────────────────────────────────────────────────────────────

    [Fact]
    public void Refund_AfterUse_DecrementsUsedQuantity()
    {
        var coupon = Make(maxUsage: 5);
        coupon.Use();

        coupon.Refund();

        coupon.UsedQuantity.Should().Be(0);
    }

    [Fact]
    public void Refund_WhenAlreadyZero_DoesNotGoNegative()
    {
        var coupon = Make(maxUsage: 5);

        coupon.Refund(); // no-op

        coupon.UsedQuantity.Should().Be(0);
    }

    // ── State helpers ─────────────────────────────────────────────────────────

    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var coupon = Make(daysUntilExpiry: 1);
        coupon.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var coupon = Make(daysUntilExpiry: -1);
        coupon.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void HasRemaining_UsedLessThanMax_ReturnsTrue()
    {
        var coupon = Make(maxUsage: 5);
        coupon.HasRemaining().Should().BeTrue();
    }

    [Fact]
    public void HasRemaining_UsedEqualsMax_ReturnsFalse()
    {
        var coupon = Make(maxUsage: 1);
        coupon.Use();
        coupon.HasRemaining().Should().BeFalse();
    }

    [Fact]
    public void MeetsMinOrderValue_ExactlyMin_ReturnsTrue()
    {
        var coupon = Make(minOrderValue: 100);
        coupon.MeetsMinOrderValue(100m).Should().BeTrue();
    }

    [Fact]
    public void MeetsMinOrderValue_BelowMin_ReturnsFalse()
    {
        var coupon = Make(minOrderValue: 100);
        coupon.MeetsMinOrderValue(99m).Should().BeFalse();
    }
}
