using FluentAssertions;
using MediatR;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Features.Orders.Commands.PlaceOrder;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Orders;

public class PlaceOrderSizeInventoryTests
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

    public PlaceOrderSizeInventoryTests()
    {
        _priceCampaignRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<(Guid, Guid?)>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());
    }
    private readonly Mock<IMediator> _mediator = new();
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _addressId = Guid.NewGuid();

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _orderRepo.Object, _productRepo.Object,
            _storeRepo.Object, _storeInventoryRepo.Object, _storeSizeInventoryRepo.Object,
            _couponRepo.Object, _couponUsageRepo.Object,
            _userRepo.Object, _userAddressRepo.Object, _priceCampaignRepo.Object,
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

    /// <summary>
    /// Product with HasSizes = true, CartItem has SizeId → deducts StoreSizeInventory, not StoreInventory.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithSizedProduct_DeductsStoreSizeInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sizeId = Guid.NewGuid();

        // Build a sized product (HasSizes = true via reflection since setter is private)
        var product = Product.Create("T-Shirt", "Desc", 200m, Guid.NewGuid(), "t-shirt");
        typeof(Product).GetProperty(nameof(Product.HasSizes))!.SetValue(product, true);

        // Cart item with SizeId set
        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, "T-Shirt", null, 2, 200m, sizeId, "M");

        var sizeInventory = StoreSizeInventory.Create(_storeId, productId, sizeId, quantity: 10);
        var baseInventory = StoreInventory.Create(_storeId, productId, 10);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupDefaultAddress();

        _storeSizeInventoryRepo
            .Setup(r => r.GetByStoreAndSizesAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<StoreSizeInventory> { sizeInventory });

        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { baseInventory });

        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        sizeInventory.Quantity.Should().Be(8); // 10 - 2
        baseInventory.Quantity.Should().Be(8); // also deducted for sized products
    }

    /// <summary>
    /// Product with HasSizes = false, CartItem has no SizeId → deducts StoreInventory (not StoreSizeInventory).
    /// </summary>
    [Fact]
    public async Task PlaceOrder_WithNonSizedProduct_DeductsStoreInventory()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var product = Product.Create("Laptop", "Desc", 500m, Guid.NewGuid(), "laptop");
        // HasSizes defaults to false

        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, "Laptop", null, 1, 500m); // no sizeId

        var inventory = StoreInventory.Create(_storeId, productId, 5);

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();
        SetupDefaultAddress();

        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new[] { inventory });

        // No sized items: GetByStoreAndSizesAsync should not be called
        _storeSizeInventoryRepo
            .Setup(r => r.GetByStoreAndSizesAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<StoreSizeInventory>());

        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        inventory.Quantity.Should().Be(4); // 5 - 1

        _storeSizeInventoryRepo.Verify(
            r => r.GetByStoreAndSizesAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default),
            Times.Never);
    }

    /// <summary>
    /// Sized product, StoreSizeInventory has insufficient stock → ConflictException thrown.
    /// </summary>
    [Fact]
    public async Task PlaceOrder_InsufficientSizeInventory_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sizeId = Guid.NewGuid();

        var product = Product.Create("Hoodie", "Desc", 350m, Guid.NewGuid(), "hoodie");
        typeof(Product).GetProperty(nameof(Product.HasSizes))!.SetValue(product, true);

        var cart = CartEntity.Create(userId);
        cart.AddItem(productId, "Hoodie", null, 5, 350m, sizeId, "XL"); // wants 5

        var sizeInventory = StoreSizeInventory.Create(_storeId, productId, sizeId, quantity: 2); // only 2 in stock

        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        SetupActiveStore();

        _storeSizeInventoryRepo
            .Setup(r => r.GetByStoreAndSizesAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(new List<StoreSizeInventory> { sizeInventory });

        _storeInventoryRepo
            .Setup(r => r.GetByStoreAndProductsAsync(_storeId, It.IsAny<IEnumerable<Guid>>(), default))
            .ReturnsAsync(Array.Empty<StoreInventory>());

        var act = () => CreateHandler().Handle(ValidCommand(userId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
