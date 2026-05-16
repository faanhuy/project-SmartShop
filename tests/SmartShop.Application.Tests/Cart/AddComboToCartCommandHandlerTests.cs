using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Cart.Commands.AddComboToCart;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Application.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Cart;

public class AddComboToCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IComboRepository> _comboRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AddComboToCartCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _comboRepo.Object, _uow.Object);

    private static Combo CreateTestCombo(Guid productId, decimal salePrice = 99.99m)
    {
        var combo = Combo.Create(
            "Test Combo",
            "Test Combo Title",
            "Description",
            "image.jpg",
            salePrice,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)
        );

        var product = Product.Create(
            "Test Product",
            "Description",
            50m,
            Guid.NewGuid(),
            "test-product"
        );

        var item = ComboItem.Create(combo.Id, productId, product.Name, null, null, 1, 50m);
        combo.AddItem(item);

        return combo;
    }

    [Fact]
    public async Task Handle_NewCart_CreatesCartWithComboItem()
    {
        var userId = Guid.NewGuid();
        var comboId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(() => callCount++ == 0 ? null : CartEntity.Create(userId));
        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        _cartRepo.Verify(r => r.AddAsync(It.IsAny<CartEntity>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ExistingCart_NewCombo_AddsCartItemAndComponents()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);
        var existingCart = CartEntity.Create(userId);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(() => callCount++ == 0 ? existingCart : CartEntity.Create(userId));
        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new AddComboToCartCommand(userId, combo.Id, 2), default);

        _cartRepo.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>(), default), Times.Once);
        _cartRepo.Verify(r => r.AddCartItemComponentAsync(It.IsAny<CartItemComponent>(), default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ComboNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var comboId = Guid.NewGuid();

        _comboRepo.Setup(r => r.GetByIdAsync(comboId, default))
            .ReturnsAsync((Combo?)null);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, comboId, 1), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ComboInactive_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);
        combo.Deactivate();

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*không còn khả dụng*");
    }

    [Fact]
    public async Task Handle_ComboExpired_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = Combo.Create(
            "Test Combo",
            "Test Combo Title",
            "Description",
            "image.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1) // Expired yesterday
        );

        var item = ComboItem.Create(combo.Id, productId, "Product", null, null, 1, 50m);
        combo.AddItem(item);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*không còn khả dụng*");
    }

    [Fact]
    public async Task Handle_ComboNoItems_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var combo = Combo.Create(
            "Empty Combo",
            "Title",
            "Description",
            "image.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)
        );
        // combo has no items

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*không có món con*");
    }

    [Fact]
    public async Task Handle_ExistingCart_DuplicateCombo_DoesNotAddNewItem()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);

        var existingCart = CartEntity.Create(userId);
        // Add combo to cart first
        var cartItem = CartItem.CreateCombo(existingCart.Id, combo.Id, combo.Title, combo.ImageUrl, 1, combo.SalePrice);
        var component = CartItemComponent.Create(cartItem.Id, productId, "Product", null, null, 1, 1, 50m);
        cartItem.AddComponent(component);
        existingCart.AddComboItem(combo.Id, combo.Title, combo.ImageUrl, 1, combo.SalePrice, new[] { component });

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(() => callCount++ == 0 ? existingCart : CartEntity.Create(userId));
        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new AddComboToCartCommand(userId, combo.Id, 1), default);

        // AddCartItemAsync should not be called (not a new item)
        _cartRepo.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCombo_ReturnsMappedCartDto()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(() => callCount++ == 0 ? null : CartEntity.Create(userId));
        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_ComboStartsInFuture_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = Combo.Create(
            "Future Combo",
            "Title",
            "Description",
            "image.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(5), // Starts in future
            DateTime.UtcNow.AddDays(30)
        );

        var item = ComboItem.Create(combo.Id, productId, "Product", null, null, 1, 50m);
        combo.AddItem(item);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 1), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*không còn khả dụng*");
    }

    [Fact]
    public async Task Handle_InvalidQuantity_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var combo = CreateTestCombo(productId, 99.99m);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(
            new AddComboToCartCommand(userId, combo.Id, 0), default);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Số lượng phải lớn hơn 0*");
    }

    [Fact]
    public async Task Handle_ExistingCart_ComboWithMultipleProducts_AddsAllComponents()
    {
        var userId = Guid.NewGuid();
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        var combo = Combo.Create(
            "Multi Combo",
            "Title",
            "Description",
            "image.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(30)
        );

        combo.AddItem(ComboItem.Create(combo.Id, product1Id, "Product1", null, null, 2, 50m));
        combo.AddItem(ComboItem.Create(combo.Id, product2Id, "Product2", null, null, 1, 75m));

        var existingCart = CartEntity.Create(userId);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
            .ReturnsAsync(() => callCount++ == 0 ? existingCart : CartEntity.Create(userId));
        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new AddComboToCartCommand(userId, combo.Id, 1), default);

        // Should add 1 cart item and 2 components
        _cartRepo.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>(), default), Times.Once);
        _cartRepo.Verify(r => r.AddCartItemComponentAsync(It.IsAny<CartItemComponent>(), default), Times.Exactly(2));
    }
}
