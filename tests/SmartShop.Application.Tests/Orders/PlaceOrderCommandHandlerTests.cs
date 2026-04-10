using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
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
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlaceOrderCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _orderRepo.Object, _productRepo.Object, _uow.Object);

    private static PlaceOrderCommand ValidCommand(Guid userId) =>
        new(userId, "123 Main St", null);

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
}
