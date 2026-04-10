using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
using Xunit;
using SmartShop.Application.Features.Cart.Commands.AddToCart;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Cart;

public class AddToCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AddToCartCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _productRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_NewCart_CreatesCartAndAddsItem()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 10, Guid.NewGuid(), "product-slug");

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        // First call: no existing cart; second call (post-save): return empty cart for DTO
        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                 .ReturnsAsync(() => callCount++ == 0 ? null : CartEntity.Create(userId));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new AddToCartCommand(userId, productId, 1), default);

        _cartRepo.Verify(r => r.AddAsync(It.IsAny<CartEntity>(), default), Times.Once);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ExistingCart_NewItem_CallsAddCartItem()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 10, Guid.NewGuid(), "product-slug");
        var existingCart = CartEntity.Create(userId);

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                 .ReturnsAsync(() => callCount++ == 0 ? existingCart : CartEntity.Create(userId));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new AddToCartCommand(userId, productId, 2), default);

        _cartRepo.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCart_ExistingItem_DoesNotCallAddCartItem()
    {
        var userId = Guid.NewGuid();
        // Create product first so we can use its actual Id (handler uses product.Id, not request.ProductId)
        var product = Product.Create("Product", "Desc", 50m, 10, Guid.NewGuid(), "product-slug");
        var productId = product.Id;
        var existingCart = CartEntity.Create(userId);
        existingCart.AddItem(product.Id, 1, 50m); // already has this product

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                 .ReturnsAsync(() => callCount++ == 0 ? existingCart : CartEntity.Create(userId));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new AddToCartCommand(userId, productId, 1), default);

        _cartRepo.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(new AddToCartCommand(userId, productId, 1), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveProduct_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 10, Guid.NewGuid(), "product-slug");
        product.Deactivate();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var act = () => CreateHandler().Handle(new AddToCartCommand(userId, productId, 1), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_QuantityExceedsStock_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 3, Guid.NewGuid(), "product-slug");
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var act = () => CreateHandler().Handle(new AddToCartCommand(userId, productId, 10), default);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
