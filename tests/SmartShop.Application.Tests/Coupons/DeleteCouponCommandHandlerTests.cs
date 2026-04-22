using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Features.Coupons.Commands.DeleteCoupon;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.Coupons;

public class DeleteCouponCommandHandlerTests
{
    private readonly Mock<ICouponRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    private DeleteCouponCommandHandler CreateHandler() =>
        new(_repo.Object, _uow.Object, _cache.Object);

    private static Coupon MakeCoupon(string code = "PROMO") =>
        Coupon.Create(code, DiscountType.FixedAmount, 30, DateTime.UtcNow.AddDays(7), 10, "desc", 50);

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnusedCoupon_DeletesAndSavesChanges()
    {
        var coupon = MakeCoupon("PROMO");
        _repo.Setup(r => r.GetByCodeAsync("PROMO", default)).ReturnsAsync(coupon);
        _repo.Setup(r => r.HasAnyUsageAsync(coupon.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteCouponCommand("PROMO"), default);

        _repo.Verify(r => r.DeleteAsync(coupon, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_UnusedCoupon_InvalidatesBothCacheKeys()
    {
        var coupon = MakeCoupon("PROMO");
        _repo.Setup(r => r.GetByCodeAsync("PROMO", default)).ReturnsAsync(coupon);
        _repo.Setup(r => r.HasAnyUsageAsync(coupon.Id, default)).ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteCouponCommand("PROMO"), default);

        _cache.Verify(c => c.RemoveAsync("coupons:id:PROMO", default), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("coupons:list:", default), Times.Once);
    }

    // ── Unhappy paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CouponNotFound_ThrowsNotFoundException()
    {
        _repo.Setup(r => r.GetByCodeAsync("GHOST", default)).ReturnsAsync((Coupon?)null);

        var act = () => CreateHandler().Handle(new DeleteCouponCommand("GHOST"), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CouponHasUsage_ThrowsConflictException()
    {
        var coupon = MakeCoupon("USED");
        _repo.Setup(r => r.GetByCodeAsync("USED", default)).ReturnsAsync(coupon);
        _repo.Setup(r => r.HasAnyUsageAsync(coupon.Id, default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new DeleteCouponCommand("USED"), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*USED*");
    }

    [Fact]
    public async Task Handle_CouponHasUsage_DoesNotDeleteOrInvalidateCache()
    {
        var coupon = MakeCoupon("USED");
        _repo.Setup(r => r.GetByCodeAsync("USED", default)).ReturnsAsync(coupon);
        _repo.Setup(r => r.HasAnyUsageAsync(coupon.Id, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(new DeleteCouponCommand("USED"), default));

        _repo.Verify(r => r.DeleteAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), default), Times.Never);
    }
}
