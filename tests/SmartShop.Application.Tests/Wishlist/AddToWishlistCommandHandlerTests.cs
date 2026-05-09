using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Features.Wishlist.Commands.AddToWishlist;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Wishlist;

public class AddToWishlistCommandHandlerTests
{
    private readonly Mock<IWishlistRepository> _wishlistRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private static readonly string UserId = Guid.NewGuid().ToString();

    public AddToWishlistCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(UserId);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
    }

    private AddToWishlistCommandHandler CreateHandler() =>
        new(_wishlistRepo.Object, _productRepo.Object, _uow.Object, _currentUser.Object);

    private static Product CreateProduct() =>
        Product.Create("San pham A", "Mo ta", 100m, Guid.NewGuid(), "san-pham-a");

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _wishlistRepo.Setup(r => r.ExistsAsync(UserId, productId, default)).ReturnsAsync(false);

        var result = await CreateHandler().Handle(new AddToWishlistCommand(productId), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsItemAndSaves()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _wishlistRepo.Setup(r => r.ExistsAsync(UserId, productId, default)).ReturnsAsync(false);

        await CreateHandler().Handle(new AddToWishlistCommand(productId), default);

        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(new AddToWishlistCommand(productId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ProductAlreadyInWishlist_ThrowsConflictException()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _wishlistRepo.Setup(r => r.ExistsAsync(UserId, productId, default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new AddToWishlistCommand(productId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ProductAlreadyInWishlist_DoesNotCallAddOrSave()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _wishlistRepo.Setup(r => r.ExistsAsync(UserId, productId, default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new AddToWishlistCommand(productId), default);

        await act.Should().ThrowAsync<ConflictException>();
        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), default), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }
}
