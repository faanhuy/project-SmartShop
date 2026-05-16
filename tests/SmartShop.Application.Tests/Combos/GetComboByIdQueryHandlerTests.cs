using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Features.Combos.Queries.GetComboById;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Combos;

public class GetComboByIdQueryHandlerTests
{
    private readonly Mock<IComboRepository> _comboRepo = new();

    private GetComboByIdQueryHandler CreateHandler() => new(_comboRepo.Object);

    private static Combo CreateTestCombo(int itemCount = 2)
    {
        var combo = Combo.Create("Test Combo", "Test Title", "Description", "image.jpg", 99.99m, DateTime.UtcNow.AddDays(1));
        for (int i = 0; i < itemCount; i++)
        {
            var item = ComboItem.Create(combo.Id, Guid.NewGuid(), $"Product {i}", null, null, 1, 50m);
            combo.AddItem(item);
        }
        return combo;
    }

    [Fact]
    public async Task Handle_ComboExists_ReturnsComboDto()
    {
        var combo = CreateTestCombo(2);
        _comboRepo.Setup(r => r.GetByIdWithProductsAsync(combo.Id, default)).ReturnsAsync(combo);

        var result = await CreateHandler().Handle(new GetComboByIdQuery(combo.Id), default);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().Be(combo.Id);
        result.Data.Name.Should().Be(combo.Name);
        result.Data.Title.Should().Be(combo.Title);
        result.Data.Description.Should().Be(combo.Description);
        result.Data.ImageUrl.Should().Be(combo.ImageUrl);
    }

    [Fact]
    public async Task Handle_ComboExists_MapsItemsCorrectly()
    {
        var combo = CreateTestCombo(2);
        _comboRepo.Setup(r => r.GetByIdWithProductsAsync(combo.Id, default)).ReturnsAsync(combo);

        var result = await CreateHandler().Handle(new GetComboByIdQuery(combo.Id), default);

        result.Data.Should().NotBeNull();
        result.Data.Items.Count.Should().Be(2);
        result.Data.Items[0].ProductName.Should().Be("Product 0");
        result.Data.Items[1].ProductName.Should().Be("Product 1");
    }

    [Fact]
    public async Task Handle_ComboNotFound_ThrowsNotFoundException()
    {
        var comboId = Guid.NewGuid();
        _comboRepo.Setup(r => r.GetByIdWithProductsAsync(comboId, default)).ReturnsAsync((Combo?)null);

        var act = () => CreateHandler().Handle(new GetComboByIdQuery(comboId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ComboExists_IncludesIsCurrentlyActive()
    {
        var combo = Combo.Create("Active Combo", "Title", "Desc", "img.jpg", 99.99m,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30));
        _comboRepo.Setup(r => r.GetByIdWithProductsAsync(combo.Id, default)).ReturnsAsync(combo);

        var result = await CreateHandler().Handle(new GetComboByIdQuery(combo.Id), default);

        result.Data.Should().NotBeNull();
        result.Data.IsCurrentlyActive.Should().BeTrue(); // StartsAt = UtcNow.AddDays(1) but logic checks <= now
    }
}
