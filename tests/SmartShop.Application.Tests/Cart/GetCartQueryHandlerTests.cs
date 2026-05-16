using FluentAssertions;
using Moq;
using SmartShop.Application.Features.Cart.Queries.GetCart;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using Xunit;
using CartEntity = SmartShop.Domain.Entities.Cart;

namespace SmartShop.Application.Tests.Cart;

public class GetCartQueryHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepo = new();

    private GetCartQueryHandler CreateHandler() => new(_cartRepo.Object);

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDto()
    {
        var userId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var result = await CreateHandler().Handle(new GetCartQuery(userId), default);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_NoCart_ReturnsEmptyCartDto()
    {
        var userId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync((CartEntity?)null);

        var result = await CreateHandler().Handle(new GetCartQuery(userId), default);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Items.Should().BeEmpty();
        result.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CartWithItems_ReturnsTotalAmount()
    {
        var userId = Guid.NewGuid();
        var cart = CartEntity.Create(userId);
        var productId = Guid.NewGuid();
        cart.AddItem(productId, "P", null, 1, 100m);
        _cartRepo.Setup(r => r.GetByUserIdAsync(userId, default)).ReturnsAsync(cart);

        var result = await CreateHandler().Handle(new GetCartQuery(userId), default);

        result.TotalAmount.Should().Be(100m);
        result.Items.Should().HaveCount(1);
    }
}
