using FluentAssertions;
using MediatR;
using Moq;
using SmartShop.Application.Services;
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
    private readonly Mock<IStoreRepository> _storeRepo = new();
    private readonly Mock<IStoreInventoryRepository> _storeInventoryRepo = new();
    private readonly Mock<IStoreSizeInventoryRepository> _storeSizeInventoryRepo = new();
    private readonly Mock<ICouponRepository> _couponRepo = new();
    private readonly Mock<ICouponUsageRepository> _couponUsageRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUserAddressRepository> _userAddressRepo = new();
    private readonly Mock<IPriceCampaignRepository> _priceCampaignRepo = new();
    private readonly Mock<IComboPromotionService> _comboService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _addressId = Guid.NewGuid();

    public PlaceOrderCommandHandlerTests()
    {
        // Default: no effective price rules
        _priceCampaignRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<(Guid, Guid?)>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        // Default: no combo match
        _comboService
            .Setup(s => s.FindApplicableComboAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<CartItemInput>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComboMatchResult?)null);
    }

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _orderRepo.Object, _productRepo.Object,
            _storeRepo.Object, _storeInventoryRepo.Object, _storeSizeInventoryRepo.Object,
            _couponRepo.Object, _couponUsageRepo.Object,
            _userRepo.Object, _userAddressRepo.Object,
            _priceCampaignRepo.Object, _comboService.Object,
            _uow.Object, _mediator.Object);

    private PlaceOrderCommand ValidCommand(Guid userId, string? couponCode = null) =>
        new(userId, _storeId, _addressId, null, couponCode);

    private void SetupActiveStore()
    {
        var store = Store.Create("Store", "Addr", "123");
        _storeRepo.Setup(r => r.GetByIdAsync(_storeId, default)).ReturnsAsync(store);
    }

    private void SetupInventory(Guid productId, int quantity)
    {
        var inventory = StoreInventory.Create(_storeId, productId, quantity);
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });
    }

    private void SetupDefaultAddress()
    {
        var address = UserAddress.Create(
            Guid.NewGuid().ToString(), "Home", "Test User", "0901234567",
            "123 Main St", null, "Q1", "TP.HCM");
        _userAddressRepo.Setup(r => r.GetByIdAsync(_addressId, default)).ReturnsAsync(address);
    }

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderAndReturnsDto()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
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
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        product.Deactivate();

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        SetupActiveStore();
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
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 2); // only 2 in stock

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidCart_ReducesInventoryStock()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        var inventory = StoreInventory.Create(_storeId, productId, 10);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });
        SetupDefaultAddress();
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(userId), default);

        inventory.Quantity.Should().Be(8); // 10 - 2
    }

    [Fact]
    public async Task Handle_ValidCart_ClearsCartAfterOrder()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 5);
        SetupDefaultAddress();
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(userId), default);

        cart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCoupon_AppliesDiscountAndSavesUsage()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 2, 100m); // 200k
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("SALE10", SmartShop.Domain.Enums.DiscountType.Percentage, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 100);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
        _couponRepo.Setup(r => r.GetByCodeAsync("SALE10", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId, "SALE10"), default);

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
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("EXPIRED", SmartShop.Domain.Enums.DiscountType.FixedAmount, 50, DateTime.UtcNow.AddDays(-1), 5, "desc", 50);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
        _couponRepo.Setup(r => r.GetByCodeAsync("EXPIRED", default)).ReturnsAsync(coupon);

        var act = () => CreateHandler().Handle(ValidCommand(userId, "EXPIRED"), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết hạn*");
    }

    [Fact]
    public async Task Handle_CouponAlreadyUsedByUser_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("ONCE", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 50);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
        _couponRepo.Setup(r => r.GetByCodeAsync("ONCE", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(ValidCommand(userId, "ONCE"), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*đã sử dụng*");
    }

    [Fact]
    public async Task Handle_CouponBelowMinOrderValue_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 40m); // chỉ 40k
        var product = Product.Create("Laptop", "Desc", 40m, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("MIN100", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 5, "desc", 100);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
        _couponRepo.Setup(r => r.GetByCodeAsync("MIN100", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var act = () => CreateHandler().Handle(ValidCommand(userId, "MIN100"), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*tối thiểu*");
    }

    [Fact]
    public async Task Handle_CouponNoRemainingUsage_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");
        var coupon = Coupon.Create("LIMITED", SmartShop.Domain.Enums.DiscountType.FixedAmount, 10, DateTime.UtcNow.AddDays(1), 1, "desc", 50);
        // Đã dùng hết lượt
        typeof(Coupon).GetProperty("UsedQuantity")!.SetValue(coupon, 1);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        SetupDefaultAddress();
        _couponRepo.Setup(r => r.GetByCodeAsync("LIMITED", default)).ReturnsAsync(coupon);
        _couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var act = () => CreateHandler().Handle(ValidCommand(userId, "LIMITED"), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*hết lượt*");
    }

    [Fact]
    public async Task Handle_WithStructuredShipping_SetsSnapshotFields()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, 1, 100m);
        var product = Product.Create("Laptop", "Desc", 100m, Guid.NewGuid(), "laptop");

        var address = UserAddress.Create(
            userId.ToString(), "Home", "Test User", "0901234567",
            "123 Đường Lê Lợi", "Phường Bến Nghé", "Q1", "TP. Hồ Chí Minh");
        _userAddressRepo.Setup(r => r.GetByIdAsync(_addressId, default)).ReturnsAsync(address);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupInventory(productId, 10);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        result.ShippingStreet.Should().Be("123 Đường Lê Lợi");
        result.ShippingWardName.Should().Be("Phường Bến Nghé");
        result.ShippingProvinceName.Should().Be("TP. Hồ Chí Minh");
    }
}
