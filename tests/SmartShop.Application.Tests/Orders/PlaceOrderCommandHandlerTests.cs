using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Features.Orders.Commands.PlaceOrder;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Orders;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICouponRepository> _couponRepo = new();
    private readonly Mock<ICouponUsageRepository> _couponUsageRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _orderRepo.Object, _productRepo.Object, _couponRepo.Object, _couponUsageRepo.Object, _uow.Object);
    private static PlaceOrderCommand ValidCommand(Guid userId) =>
        new(userId, "123 Main St", null, null);

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TotalAmount.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_CartNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((CartEntity?)null);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_EmptyCart_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var cart = CartEntity.Create(userId); // no items
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_InactiveProduct_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");
        product.Deactivate();

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_InsufficientStock_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 5, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 2, Guid.NewGuid(), "laptop"); // only 2 in stock

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidCart_ReducesProductStock()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(userId), default);

        _productRepo.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
        product.Stock.Should().Be(8); // 10 - 2
    }

    [Fact]
    public async Task Handle_ValidCart_ClearsCartAfterOrder()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 5, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(userId), default);

        cart.Items.Should().BeEmpty();
    }
    [Fact]
    public async Task Handle_ValidCoupon_AppliesDiscountAndSavesUsage()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var couponId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m); // 200k
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("SALE10", SmartShop.Domain.Enums.DiscountType.Percentage, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 100);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _couponRepo.Setup(r => r.GetByCodeAsync("SALE10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var cmd = new PlaceOrderCommand(userId, "123 Main St", null, "SALE10");
        var result = await CreateHandler().Handle(cmd, default);

        result.CouponCode.Should().Be("SALE10");
        result.DiscountAmount.Should().Be(20); // 10% of 200
        _couponUsageRepo.Verify(r => r.AddAsync(It.IsAny<CouponUsage>(), default), Times.Once);
        _couponRepo.Verify(r => r.Update(It.IsAny<Coupon>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredCoupon_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("EXPIRED", SmartShop.Domain.Enums.DiscountType.FixedAmount, 50, DateTime.UtcNow.AddDays(-1), 5, "desc", 50);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _couponRepo.Setup(r => r.GetByCodeAsync("EXPIRED", default)).ReturnsAsync(coupon);

        var cmd = new PlaceOrderCommand(userId, "123 Main St", null, "EXPIRED");
        var act = () => CreateHandler().Handle(cmd, default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết hạn*");
    }

    [Fact]
    public async Task Handle_CouponAlreadyUsedByUser_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("ONCE", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 50);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _couponRepo.Setup(r => r.GetByCodeAsync("ONCE", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(true);

        var cmd = new PlaceOrderCommand(userId, "123 Main St", null, "ONCE");
        var act = () => CreateHandler().Handle(cmd, default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*đã sử dụng*");
    }

    [Fact]
    public async Task Handle_CouponBelowMinOrderValue_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 40m); // chỉ 40k
        var product = Product.Create("Laptop", "Desc", 40m, 10, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("MIN100", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 100);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _couponRepo.Setup(r => r.GetByCodeAsync("MIN100", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var cmd = new PlaceOrderCommand(userId, "123 Main St", null, "MIN100");
        var act = () => CreateHandler().Handle(cmd, default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*tối thiểu*");
    }

    [Fact]
    public async Task Handle_CouponNoRemainingUsage_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, 10, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("LIMITED", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 1, "desc", 50);
        // Đã dùng hết lượt
        typeof(Coupon).GetProperty("UsedQuantity")!.SetValue(coupon, 1);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _couponRepo.Setup(r => r.GetByCodeAsync("LIMITED", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var cmd = new PlaceOrderCommand(userId, "123 Main St", null, "LIMITED");
        var act = () => CreateHandler().Handle(cmd, default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết lượt*");
    }
}
