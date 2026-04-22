using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Features.Coupons.Commands.CreateCoupon;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.Coupons;

public class CreateCouponCommandHandlerTests
{
    private readonly Mock<ICouponRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    private CreateCouponCommandHandler CreateHandler() =>
        new(_repo.Object, _uow.Object, _cache.Object);

    private static CreateCouponCommand ValidCommand(string code = "SAVE20") =>
        new(code, DiscountType.Percentage, 20, 100, 10, DateTime.UtcNow.AddDays(30), "Test coupon");

    // ── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NewCode_CreatesCouponAndReturnsResponse()
    {
        _repo.Setup(r => r.GetByCodeAsync("SAVE20", default)).ReturnsAsync((Coupon?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(), default);

        result.Should().NotBeNull();
        result.Code.Should().Be("SAVE20");
        result.DiscountType.Should().Be(DiscountType.Percentage);
        result.DiscountValue.Should().Be(20);
        result.UsedQuantity.Should().Be(0);

        _repo.Verify(r => r.AddAsync(It.Is<Coupon>(c => c.Code == "SAVE20"), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NewCode_InvalidatesCouponListCache()
    {
        _repo.Setup(r => r.GetByCodeAsync("SAVE20", default)).ReturnsAsync((Coupon?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(), default);

        _cache.Verify(c => c.RemoveByPrefixAsync("coupons:list:", default), Times.Once);
    }

    [Fact]
    public async Task Handle_FixedAmountType_SetsDiscountTypeCorrectly()
    {
        var cmd = new CreateCouponCommand("FIXED50", DiscountType.FixedAmount, 50, 200, 5, DateTime.UtcNow.AddDays(7), null);

        _repo.Setup(r => r.GetByCodeAsync("FIXED50", default)).ReturnsAsync((Coupon?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(cmd, default);

        result.DiscountType.Should().Be(DiscountType.FixedAmount);
        result.DiscountValue.Should().Be(50);
    }

    // ── Unhappy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsConflictException()
    {
        var existing = Coupon.Create("SAVE20", DiscountType.Percentage, 20, DateTime.UtcNow.AddDays(30), 10, "old", 100);
        _repo.Setup(r => r.GetByCodeAsync("SAVE20", default)).ReturnsAsync(existing);

        var act = () => CreateHandler().Handle(ValidCommand(), default);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*SAVE20*");
    }

    [Fact]
    public async Task Handle_DuplicateCode_DoesNotPersistOrInvalidateCache()
    {
        var existing = Coupon.Create("SAVE20", DiscountType.Percentage, 20, DateTime.UtcNow.AddDays(30), 10, "old", 100);
        _repo.Setup(r => r.GetByCodeAsync("SAVE20", default)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<ConflictException>(() => CreateHandler().Handle(ValidCommand(), default));

        _repo.Verify(r => r.AddAsync(It.IsAny<Coupon>(), default), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
        _cache.Verify(c => c.RemoveByPrefixAsync(It.IsAny<string>(), default), Times.Never);
    }
}
