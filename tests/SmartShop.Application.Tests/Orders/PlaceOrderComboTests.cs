using FluentAssertions;
using MediatR;
using Moq;
using Xunit;
using SmartShop.Application.Features.Orders.Commands.PlaceOrder;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Application.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Orders;

public class PlaceOrderComboTests
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
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _addressId = Guid.NewGuid();

    public PlaceOrderComboTests()
    {
        // Default: no effective price rules
        _priceCampaignRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<(Guid, Guid?)>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());
    }

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _orderRepo.Object, _productRepo.Object,
            _storeRepo.Object, _storeInventoryRepo.Object, _storeSizeInventoryRepo.Object,
            _couponRepo.Object, _couponUsageRepo.Object,
            _userRepo.Object, _userAddressRepo.Object,
            _priceCampaignRepo.Object,
            _uow.Object, _mediator.Object);

    private PlaceOrderCommand ValidCommand(Guid userId) =>
        new(userId, _storeId, _addressId, null, null);

    private void SetupActiveStore()
    {
        var store = Store.Create("Store", "Addr", "123");
        _storeRepo.Setup(r => r.GetByIdAsync(_storeId, default)).ReturnsAsync(store);
    }

    private void SetupDefaultAddress()
    {
        var address = UserAddress.Create(
            Guid.NewGuid().ToString(), "Home", "Test User", "0901234567",
            "123 Main St", null, "Q1", "TP.HCM");
        _userAddressRepo.Setup(r => r.GetByIdAsync(_addressId, default)).ReturnsAsync(address);
    }

    private void SetupInventory(Guid productId, int quantity)
    {
        var inventory = StoreInventory.Create(_storeId, productId, quantity);
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });
    }

    private static CartItem CreateComboCartItem(
        Guid cartId,
        Guid comboId,
        int quantity,
        decimal salePrice,
        List<(Guid productId, string name, int qtyPerCombo, decimal unitPrice)> components)
    {
        var cartItem = CartItem.CreateCombo(cartId, comboId, "Combo Name", null, quantity, salePrice);
        foreach (var (pid, name, qtyPerCombo, unitPrice) in components)
        {
            var comp = CartItemComponent.Create(cartItem.Id, pid, name, null, null, qtyPerCombo, quantity, unitPrice);
            cartItem.AddComponent(comp);
        }
        return cartItem;
    }

    [Fact]
    public async Task Handle_CartWithComboItem_CreatesOrderSuccessfully()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Combo Product", "Desc", 50m, Guid.NewGuid(), "combo-product");

        var cart = CartEntity.Create(userId);
        var comboItem = CreateComboCartItem(
            cart.Id,
            Guid.NewGuid(),
            quantity: 1,
            salePrice: 99.99m,
            components: new List<(Guid, string, int, decimal)>
            {
                (productId, "Product", 1, 50m)
            }
        );
        cart.AddComboItem(comboItem.ComboId!.Value, "Combo", null, 1, 99.99m, comboItem.Components);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupDefaultAddress();
        SetupInventory(productId, 10);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_ComboItem_DeductsComponentInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Combo Product", "Desc", 50m, Guid.NewGuid(), "combo-product");

        var cart = CartEntity.Create(userId);
        var comboItem = CreateComboCartItem(
            cart.Id,
            Guid.NewGuid(),
            quantity: 2, // Quantity 2
            salePrice: 99.99m,
            components: new List<(Guid, string, int, decimal)>
            {
                (productId, "Product", 1, 50m) // qty_per_combo = 1, so total = 1 * 2 = 2
            }
        );
        cart.AddComboItem(comboItem.ComboId!.Value, "Combo", null, 2, 99.99m, comboItem.Components);

        var inventory = StoreInventory.Create(_storeId, productId, 10);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupDefaultAddress();
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });

        await CreateHandler().Handle(ValidCommand(userId), default);

        // Verify inventory was deducted by 2 (1 per combo * 2 combo quantity)
        inventory.Quantity.Should().Be(8); // 10 - 2
    }

    [Fact]
    public async Task Handle_ComboItem_InsufficientComponentStock_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Combo Product", "Desc", 50m, Guid.NewGuid(), "combo-product");

        var cart = CartEntity.Create(userId);
        var comboItem = CreateComboCartItem(
            cart.Id,
            Guid.NewGuid(),
            quantity: 2, // Need 2 units (1 per combo * qty 2)
            salePrice: 99.99m,
            components: new List<(Guid, string, int, decimal)>
            {
                (productId, "Product", 1, 50m) // qty_per_combo = 1
            }
        );
        cart.AddComboItem(comboItem.ComboId!.Value, "Combo", null, 2, 99.99m, comboItem.Components);

        var inventory = StoreInventory.Create(_storeId, productId, 1); // Only 1 unit available

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupDefaultAddress();
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_MixedCart_ProductAndCombo_DeductsAllInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var comboProductId = Guid.NewGuid();

        var product = Product.Create("Regular Product", "Desc", 100m, Guid.NewGuid(), "regular-product");
        var comboProduct = Product.Create("Combo Product", "Desc", 50m, Guid.NewGuid(), "combo-product");

        var cart = CartEntity.Create(userId);
        // Add regular product item
        cart.AddItem(productId, "Regular", null, 1, 100m);

        // Add combo item
        var comboItem = CreateComboCartItem(
            cart.Id,
            Guid.NewGuid(),
            quantity: 1,
            salePrice: 99.99m,
            components: new List<(Guid, string, int, decimal)>
            {
                (comboProductId, "Combo Product", 1, 50m)
            }
        );
        cart.AddComboItem(comboItem.ComboId!.Value, "Combo", null, 1, 99.99m, comboItem.Components);

        var productInventory = StoreInventory.Create(_storeId, productId, 10);
        var comboProductInventory = StoreInventory.Create(_storeId, comboProductId, 5);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _productRepo.Setup(r => r.GetByIdAsync(comboProductId, default)).ReturnsAsync(comboProduct);
        SetupActiveStore();
        SetupDefaultAddress();
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { productInventory, comboProductInventory });

        await CreateHandler().Handle(ValidCommand(userId), default);

        // Both inventories should be deducted
        productInventory.Quantity.Should().Be(9); // 10 - 1
        comboProductInventory.Quantity.Should().Be(4); // 5 - 1
    }

    [Fact]
    public async Task Handle_ComboWithMultipleComponents_DeductsAllComponentInventory()
    {
        var userId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();
        var product3Id = Guid.NewGuid();

        var product1 = Product.Create("Product1", "Desc", 30m, Guid.NewGuid(), "product1");
        var product2 = Product.Create("Product2", "Desc", 40m, Guid.NewGuid(), "product2");
        var product3 = Product.Create("Product3", "Desc", 50m, Guid.NewGuid(), "product3");

        var cart = CartEntity.Create(userId);
        var comboItem = CreateComboCartItem(
            cart.Id,
            Guid.NewGuid(),
            quantity: 2,
            salePrice: 199.99m,
            components: new List<(Guid, string, int, decimal)>
            {
                (product1Id, "Product1", 1, 30m), // 1 * 2 = 2
                (product2Id, "Product2", 2, 40m), // 2 * 2 = 4
                (product3Id, "Product3", 1, 50m)  // 1 * 2 = 2
            }
        );
        cart.AddComboItem(comboItem.ComboId!.Value, "Combo", null, 2, 199.99m, comboItem.Components);

        var inv1 = StoreInventory.Create(_storeId, product1Id, 10);
        var inv2 = StoreInventory.Create(_storeId, product2Id, 10);
        var inv3 = StoreInventory.Create(_storeId, product3Id, 10);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(product1Id, default)).ReturnsAsync(product1);
        _productRepo.Setup(r => r.GetByIdAsync(product2Id, default)).ReturnsAsync(product2);
        _productRepo.Setup(r => r.GetByIdAsync(product3Id, default)).ReturnsAsync(product3);
        SetupActiveStore();
        SetupDefaultAddress();
        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inv1, inv2, inv3 });

        await CreateHandler().Handle(ValidCommand(userId), default);

        inv1.Quantity.Should().Be(8);  // 10 - 2
        inv2.Quantity.Should().Be(6);  // 10 - 4
        inv3.Quantity.Should().Be(8);  // 10 - 2
    }

    [Fact]
    public async Task Handle_ComboItem_EmptyCart_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        // Cart is empty

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Giỏ hàng đang trống*");
    }

    [Fact]
    public async Task Handle_StoreNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(Guid.NewGuid(), "Product", null, 1, 50m);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _storeRepo.Setup(r => r.GetByIdAsync(_storeId, default))
            .ReturnsAsync((Store?)null);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveStore_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, "Product", null, 1, 50m);

        var store = Store.Create("Store", "Addr", "123");
        store.Deactivate();

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _storeRepo.Setup(r => r.GetByIdAsync(_storeId, default)).ReturnsAsync(store);

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*tạm ngừng hoạt động*");
    }
}
