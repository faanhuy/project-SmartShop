using FluentAssertions;
using Moq;
using SmartShop.Application.Services;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Services;
using Xunit;

namespace SmartShop.Application.Tests.Promotions;

public class ComboPromotionTests
{
    // ── Domain: ComboPromotion.Create ─────────────────────────────────────

    [Fact]
    public void Create_FreeProduct_Valid_ReturnsCombo()
    {
        var triggerProductId = Guid.NewGuid();
        var rewardProductId = Guid.NewGuid();

        var combo = ComboPromotion.Create(
            name: "Buy 2 Get 1",
            triggerProductId: triggerProductId,
            triggerSizeId: null,
            triggerMinQty: 2,
            rewardType: ComboRewardType.FreeProduct,
            rewardProductId: rewardProductId,
            rewardSizeId: null,
            rewardQty: 1,
            rewardAmount: null,
            storeId: null,
            startsAt: null,
            endsAt: null);

        combo.Name.Should().Be("Buy 2 Get 1");
        combo.TriggerProductId.Should().Be(triggerProductId);
        combo.TriggerMinQuantity.Should().Be(2);
        combo.RewardType.Should().Be(ComboRewardType.FreeProduct);
        combo.RewardProductId.Should().Be(rewardProductId);
        combo.RewardQuantity.Should().Be(1);
        combo.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_DiscountAmount_Valid_ReturnsCombo()
    {
        var triggerProductId = Guid.NewGuid();

        var combo = ComboPromotion.Create(
            name: "Spend 500k Save 50k",
            triggerProductId: triggerProductId,
            triggerSizeId: null,
            triggerMinQty: 3,
            rewardType: ComboRewardType.DiscountAmount,
            rewardProductId: null,
            rewardSizeId: null,
            rewardQty: null,
            rewardAmount: 50000m,
            storeId: null,
            startsAt: null,
            endsAt: null);

        combo.RewardType.Should().Be(ComboRewardType.DiscountAmount);
        combo.RewardAmount.Should().Be(50000m);
        combo.RewardProductId.Should().BeNull();
    }

    [Fact]
    public void Create_FreeProduct_MissingRewardProductId_ThrowsArgumentException()
    {
        var act = () => ComboPromotion.Create(
            name: "Invalid",
            triggerProductId: Guid.NewGuid(),
            triggerSizeId: null,
            triggerMinQty: 1,
            rewardType: ComboRewardType.FreeProduct,
            rewardProductId: null,       // missing
            rewardSizeId: null,
            rewardQty: 1,
            rewardAmount: null,
            storeId: null,
            startsAt: null,
            endsAt: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*RewardProductId*");
    }

    [Fact]
    public void Create_DiscountAmount_ZeroRewardAmount_ThrowsArgumentException()
    {
        var act = () => ComboPromotion.Create(
            name: "Invalid Discount",
            triggerProductId: Guid.NewGuid(),
            triggerSizeId: null,
            triggerMinQty: 1,
            rewardType: ComboRewardType.DiscountAmount,
            rewardProductId: null,
            rewardSizeId: null,
            rewardQty: null,
            rewardAmount: 0m,            // invalid
            storeId: null,
            startsAt: null,
            endsAt: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*RewardAmount*");
    }

    // ── Service: FindApplicableComboAsync ─────────────────────────────────

    [Fact]
    public async Task FindApplicableComboAsync_CartSatisfiesTrigger_ReturnsMatch()
    {
        var storeId = Guid.NewGuid();
        var triggerProductId = Guid.NewGuid();
        var rewardProductId = Guid.NewGuid();

        var combo = ComboPromotion.Create(
            name: "Buy 2 Get 1",
            triggerProductId: triggerProductId,
            triggerSizeId: null,
            triggerMinQty: 2,
            rewardType: ComboRewardType.FreeProduct,
            rewardProductId: rewardProductId,
            rewardSizeId: null,
            rewardQty: 1,
            rewardAmount: null,
            storeId: null,
            startsAt: null,
            endsAt: null);

        var repoMock = new Mock<IComboPromotionRepository>();
        repoMock
            .Setup(r => r.GetActiveForStoreAsync(storeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<ComboPromotion> { combo }.AsReadOnly());

        var service = new ComboPromotionService(repoMock.Object);

        var cartItems = new[]
        {
            new CartItemInput(triggerProductId, null, 3) // 3 >= TriggerMinQty=2
        };

        var result = await service.FindApplicableComboAsync(storeId, cartItems);

        result.Should().NotBeNull();
        result!.RewardType.Should().Be(ComboRewardType.FreeProduct);
        result.FreeProductId.Should().Be(rewardProductId);
        result.FreeQuantity.Should().Be(1);
    }

    [Fact]
    public async Task FindApplicableComboAsync_CartInsufficientQuantity_ReturnsNull()
    {
        var storeId = Guid.NewGuid();
        var triggerProductId = Guid.NewGuid();

        var combo = ComboPromotion.Create(
            name: "Buy 5 Get 1",
            triggerProductId: triggerProductId,
            triggerSizeId: null,
            triggerMinQty: 5,
            rewardType: ComboRewardType.FreeProduct,
            rewardProductId: Guid.NewGuid(),
            rewardSizeId: null,
            rewardQty: 1,
            rewardAmount: null,
            storeId: null,
            startsAt: null,
            endsAt: null);

        var repoMock = new Mock<IComboPromotionRepository>();
        repoMock
            .Setup(r => r.GetActiveForStoreAsync(storeId, It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<ComboPromotion> { combo }.AsReadOnly());

        var service = new ComboPromotionService(repoMock.Object);

        var cartItems = new[]
        {
            new CartItemInput(triggerProductId, null, 2) // 2 < TriggerMinQty=5
        };

        var result = await service.FindApplicableComboAsync(storeId, cartItems);

        result.Should().BeNull();
    }

    // ── PlaceOrder integration: combo FreeProduct deducts reward inventory ─

    [Fact]
    public async Task PlaceOrder_ComboFreeProduct_DeductsRewardInventoryAndAddsZeroPriceItem()
    {
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var triggerProductId = Guid.NewGuid();
        var rewardProductId = Guid.NewGuid();

        // Setup cart with trigger product
        var cart = SmartShop.Domain.Entities.Cart.Create(userId);
        cart.AddItem(triggerProductId, 2, 100m);

        var triggerProduct = Product.Create("Coffee", "Desc", 100m, Guid.NewGuid(), "coffee");
        var rewardProduct = Product.Create("Cookie", "Desc", 50m, Guid.NewGuid(), "cookie");

        var triggerInventory = StoreInventory.Create(storeId, triggerProductId, 10);
        var rewardInventory = StoreInventory.Create(storeId, rewardProductId, 5);

        var store = Store.Create("Store", "Addr", "123");

        var address = UserAddress.Create(
            userId.ToString(), "Home", "Test User", "0901234567",
            "123 Main St", null, "Q1", "TP.HCM");

        // Combo match result
        var combo = ComboPromotion.Create(
            "Buy 2 Get 1 Cookie",
            triggerProductId, null, 2,
            ComboRewardType.FreeProduct,
            rewardProductId, null, 1,
            null, null, null, null);

        var comboMatch = new ComboMatchResult(
            Combo: combo,
            RewardType: ComboRewardType.FreeProduct,
            FreeProductId: rewardProductId,
            FreeSizeId: null,
            FreeQuantity: 1,
            DiscountAmount: 0m);

        // Mocks
        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(triggerProductId, default)).ReturnsAsync(triggerProduct);
        productRepo.Setup(r => r.GetByIdAsync(rewardProductId, default)).ReturnsAsync(rewardProduct);

        var storeRepo = new Mock<IStoreRepository>();
        storeRepo.Setup(r => r.GetByIdAsync(storeId, default)).ReturnsAsync(store);

        var storeInventoryRepo = new Mock<IStoreInventoryRepository>();
        storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { triggerInventory, rewardInventory });

        var storeSizeInventoryRepo = new Mock<IStoreSizeInventoryRepository>();
        storeSizeInventoryRepo
            .Setup(r => r.GetByStoreAndSizesAsync(storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Array.Empty<StoreSizeInventory>());

        var couponRepo = new Mock<ICouponRepository>();
        var couponUsageRepo = new Mock<ICouponUsageRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userAddressRepo = new Mock<IUserAddressRepository>();
        userAddressRepo.Setup(r => r.GetByIdAsync(addressId, default)).ReturnsAsync(address);

        var priceCampaignRepo = new Mock<IPriceCampaignRepository>();
        priceCampaignRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                It.IsAny<Guid>(), It.IsAny<IEnumerable<(Guid, Guid?)>>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        var comboService = new Mock<IComboPromotionService>();
        comboService
            .Setup(s => s.FindApplicableComboAsync(storeId, It.IsAny<IEnumerable<CartItemInput>>(), default))
            .ReturnsAsync(comboMatch);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var mediator = new Mock<MediatR.IMediator>();

        var handler = new SmartShop.Application.Features.Orders.Commands.PlaceOrder.PlaceOrderCommandHandler(
            cartRepo.Object, orderRepo.Object, productRepo.Object,
            storeRepo.Object, storeInventoryRepo.Object, storeSizeInventoryRepo.Object,
            couponRepo.Object, couponUsageRepo.Object,
            userRepo.Object, userAddressRepo.Object,
            priceCampaignRepo.Object, comboService.Object,
            uow.Object, mediator.Object);

        var command = new SmartShop.Application.Features.Orders.Commands.PlaceOrder.PlaceOrderCommand(
            userId, storeId, addressId, null, null, ApplyCombo: true);

        var result = await handler.Handle(command, default);

        // Reward inventory should be deducted by 1
        rewardInventory.Quantity.Should().Be(4); // 5 - 1

        // Order should have 2 items: original + free item
        result.Items.Should().HaveCount(2);

        // Free item must have UnitPrice = 0
        var freeItem = result.Items.FirstOrDefault(i => i.ProductId == rewardProductId);
        freeItem.Should().NotBeNull();
        freeItem!.UnitPrice.Should().Be(0m);
    }

    [Fact]
    public async Task PlaceOrder_WithCoupon_SkipsComboCheck()
    {
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var cart = SmartShop.Domain.Entities.Cart.Create(userId);
        cart.AddItem(productId, 2, 100m);

        var product = Product.Create("Coffee", "Desc", 100m, Guid.NewGuid(), "coffee");
        var inventory = StoreInventory.Create(storeId, productId, 10);
        var store = Store.Create("Store", "Addr", "123");
        var address = UserAddress.Create(
            userId.ToString(), "Home", "Test User", "0901234567",
            "123 Main St", null, "Q1", "TP.HCM");
        var coupon = Coupon.Create("SAVE10", SmartShop.Domain.Enums.DiscountType.Percentage, 10,
            DateTime.UtcNow.AddDays(1), 5, "desc", 100);

        var cartRepo = new Mock<ICartRepository>();
        cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var orderRepo = new Mock<IOrderRepository>();
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var storeRepo = new Mock<IStoreRepository>();
        storeRepo.Setup(r => r.GetByIdAsync(storeId, default)).ReturnsAsync(store);

        var storeInventoryRepo = new Mock<IStoreInventoryRepository>();
        storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });

        var storeSizeInventoryRepo = new Mock<IStoreSizeInventoryRepository>();
        storeSizeInventoryRepo
            .Setup(r => r.GetByStoreAndSizesAsync(storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Array.Empty<StoreSizeInventory>());

        var couponRepo = new Mock<ICouponRepository>();
        couponRepo.Setup(r => r.GetByCodeAsync("SAVE10", default)).ReturnsAsync(coupon);
        couponRepo.Setup(r => r.HasUsageByUserAsync(coupon.Id, userId, default)).ReturnsAsync(false);

        var couponUsageRepo = new Mock<ICouponUsageRepository>();
        var userRepo = new Mock<IUserRepository>();
        var userAddressRepo = new Mock<IUserAddressRepository>();
        userAddressRepo.Setup(r => r.GetByIdAsync(addressId, default)).ReturnsAsync(address);

        var priceCampaignRepo = new Mock<IPriceCampaignRepository>();
        priceCampaignRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                It.IsAny<Guid>(), It.IsAny<IEnumerable<(Guid, Guid?)>>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        var comboService = new Mock<IComboPromotionService>();

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        var mediator = new Mock<MediatR.IMediator>();

        var handler = new SmartShop.Application.Features.Orders.Commands.PlaceOrder.PlaceOrderCommandHandler(
            cartRepo.Object, orderRepo.Object, productRepo.Object,
            storeRepo.Object, storeInventoryRepo.Object, storeSizeInventoryRepo.Object,
            couponRepo.Object, couponUsageRepo.Object,
            userRepo.Object, userAddressRepo.Object,
            priceCampaignRepo.Object, comboService.Object,
            uow.Object, mediator.Object);

        var command = new SmartShop.Application.Features.Orders.Commands.PlaceOrder.PlaceOrderCommand(
            userId, storeId, addressId, null, "SAVE10", ApplyCombo: true);

        var result = await handler.Handle(command, default);

        // ComboService should NOT have been called because coupon was provided
        comboService.Verify(
            s => s.FindApplicableComboAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<CartItemInput>>(), It.IsAny<CancellationToken>()),
            Times.Never);

        result.CouponCode.Should().Be("SAVE10");
    }
}
