using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Features.Cart.Commands.RemoveFromCart;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using Xunit;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Cart;

public class RemoveFromCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RemoveFromCartCommandHandler CreateHandler() =>
        new(_cartRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_CartNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((CartEntity?)null);

        var act = () => CreateHandler().Handle(
            new RemoveFromCartCommand(userId, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsSaveChanges()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        var product = Product.Create("P", "D", 10m, Guid.NewGuid(), "p-slug");
        cart.AddItem(productId, 1, product.Price);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                 .ReturnsAsync(() => callCount++ == 0 ? cart : CartEntity.Create(userId));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new RemoveFromCartCommand(userId, productId), default);

        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsCartDto()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        var product = Product.Create("P", "D", 10m, Guid.NewGuid(), "p-slug");
        cart.AddItem(productId, 1, product.Price);

        var callCount = 0;
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                 .ReturnsAsync(() => callCount++ == 0 ? cart : CartEntity.Create(userId));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new RemoveFromCartCommand(userId, productId), default);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
    }
}
