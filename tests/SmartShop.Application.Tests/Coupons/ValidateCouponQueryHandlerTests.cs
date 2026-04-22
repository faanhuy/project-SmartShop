using FluentAssertions;
using Moq;
using SmartShop.Application.Features.Coupons.Queries;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.Coupons;

public class ValidateCouponQueryHandlerTests
{
    private readonly Mock<ICouponRepository> _couponRepo = new();

    private ValidateCouponQueryHandler CreateHandler() =>
        new(_couponRepo.Object);

    private static Coupon ActiveCoupon(
        string code = "TEST10",
        DiscountType type = DiscountType.Percentage,
        decimal value = 10,
        decimal minOrderValue = 100,
        int maxUsage = 5,
        int daysUntilExpiry = 7) =>
        Coupon.Create(code, type, value, DateTime.UtcNow.AddDays(daysUntilExpiry), maxUsage, "desc", minOrderValue);

    // ── Happy paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidPercentageCoupon_ReturnsCorrectDiscount()
    {
        var userId = Guid.NewGuid();
        var coupon = ActiveCoupon(type: DiscountType.Percentage, value: 10, minOrderValue: 100);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var query = new ValidateCouponQuery("TEST10", 200m, userId);
        var result = await CreateHandler().Handle(query, default);

        result.DiscountAmount.Should().Be(20m);    // 10% of 200
        result.OriginalAmount.Should().Be(200m);
        result.FinalAmount.Should().Be(180m);
    }

    [Fact]
    public async Task Handle_ValidFixedAmountCoupon_ReturnsCorrectDiscount()
    {
        var userId = Guid.NewGuid();
        var coupon = ActiveCoupon(type: DiscountType.FixedAmount, value: 50, minOrderValue: 100);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var query = new ValidateCouponQuery("TEST10", 200m, userId);
        var result = await CreateHandler().Handle(query, default);

        result.DiscountAmount.Should().Be(50m);
        result.FinalAmount.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_FixedAmountExceedsOrderTotal_CapsDiscountAtOrderTotal()
    {
        var userId = Guid.NewGuid();
        // discount 200 on a 150-order — Math.Min clamps at 150
        var coupon = ActiveCoupon(type: DiscountType.FixedAmount, value: 200, minOrderValue: 100);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var query = new ValidateCouponQuery("TEST10", 150m, userId);
        var result = await CreateHandler().Handle(query, default);

        result.DiscountAmount.Should().Be(150m);
        result.FinalAmount.Should().Be(0m);
    }

    // ── Unhappy paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CouponNotFound_ThrowsNotFoundException()
    {
        _couponRepo.Setup(r => r.GetByCodeAsync("MISSING", default)).ReturnsAsync((Coupon?)null);

        var act = () => CreateHandler().Handle(new ValidateCouponQuery("MISSING", 200m, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExpiredCoupon_ThrowsConflictException()
    {
        var coupon = ActiveCoupon(daysUntilExpiry: -1);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);

        var act = () => CreateHandler().Handle(new ValidateCouponQuery("TEST10", 200m, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết hạn*");
    }

    [Fact]
    public async Task Handle_NoRemainingUsage_ThrowsConflictException()
    {
        var coupon = ActiveCoupon(maxUsage: 1);
        typeof(Coupon).GetProperty("UsedQuantity")!.SetValue(coupon, 1);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);

        var act = () => CreateHandler().Handle(new ValidateCouponQuery("TEST10", 200m, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết lượt*");
    }

    [Fact]
    public async Task Handle_OrderBelowMinValue_ThrowsConflictException()
    {
        var coupon = ActiveCoupon(minOrderValue: 500);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);

        var act = () => CreateHandler().Handle(new ValidateCouponQuery("TEST10", 200m, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*tối thiểu*");
    }

    [Fact]
    public async Task Handle_UserAlreadyUsedCoupon_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var coupon = ActiveCoupon(minOrderValue: 100);

        _couponRepo.Setup(r => r.GetByCodeAsync("TEST10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new ValidateCouponQuery("TEST10", 200m, userId), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*đã sử dụng*");
    }
}
