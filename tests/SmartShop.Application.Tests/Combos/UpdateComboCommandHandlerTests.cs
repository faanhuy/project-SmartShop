using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Features.Combos.Commands.CreateCombo;
using SmartShop.Application.Features.Combos.Commands.UpdateCombo;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Combos;

public class UpdateComboCommandHandlerTests
{
    private readonly Mock<IComboRepository> _comboRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateComboCommandHandler CreateHandler() =>
        new(_comboRepo.Object, _productRepo.Object, _uow.Object);

    private static Combo CreateTestCombo()
    {
        var combo = Combo.Create("Old Combo", "Old Title", "Old Desc", "old.jpg", 50m, DateTime.UtcNow.AddDays(1));
        var item = ComboItem.Create(combo.Id, Guid.NewGuid(), "Product", null, null, 1, 25m);
        combo.AddItem(item);
        return combo;
    }

    private static Product CreateTestProduct(Guid categoryId)
    {
        return Product.Create(
            "Test Product",
            "Description",
            50m,
            categoryId,
            "test-product"
        );
    }

    private static UpdateComboCommand ValidCommand(Guid comboId, Guid productId) =>
        new(
            comboId,
            "Updated Combo",
            "Updated Title",
            "Updated Description",
            "updated.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            new List<CreateComboItemRequest>
            {
                new(productId, null, 1)
            }
        );

    [Fact]
    public async Task Handle_ValidRequest_ReturnsUpdatedComboDto()
    {
        var combo = CreateTestCombo();
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct(categoryId);
        var command = ValidCommand(combo.Id, product.Id);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _productRepo.Setup(r => r.GetByIdWithSizesAsync(product.Id, default))
            .ReturnsAsync(product);
        _comboRepo.Setup(r => r.Update(It.IsAny<Combo>()));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, default);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        var data = result.Data;
        data.Name.Should().Be("Updated Combo");
        data.Title.Should().Be("Updated Title");
        data.Description.Should().Be("Updated Description");
        data.ImageUrl.Should().Be("updated.jpg");
    }

    [Fact]
    public async Task Handle_ComboNotFound_ThrowsNotFoundException()
    {
        var comboId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = ValidCommand(comboId, productId);

        _comboRepo.Setup(r => r.GetByIdAsync(comboId, default))
            .ReturnsAsync((Combo?)null);

        var act = () => CreateHandler().Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsConflictException()
    {
        var combo = CreateTestCombo();
        var command = new UpdateComboCommand(
            combo.Id,
            "Updated Combo",
            "Updated Title",
            null,
            "updated.jpg",
            99.99m,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(30),
            new List<CreateComboItemRequest>() // Empty
        );

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);

        var act = () => CreateHandler().Handle(command, default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Combo phải có ít nhất 1 sản phẩm*");
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var combo = CreateTestCombo();
        var productId = Guid.NewGuid();
        var command = ValidCommand(combo.Id, productId);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _productRepo.Setup(r => r.GetByIdWithSizesAsync(productId, default))
            .ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveProduct_ThrowsConflictException()
    {
        var combo = CreateTestCombo();
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct(categoryId);
        product.Deactivate();
        var command = ValidCommand(combo.Id, product.Id);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _productRepo.Setup(r => r.GetByIdWithSizesAsync(product.Id, default))
            .ReturnsAsync(product);

        var act = () => CreateHandler().Handle(command, default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*không hoạt động*");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReplacesItems()
    {
        var combo = CreateTestCombo();
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct(categoryId);
        var command = ValidCommand(combo.Id, product.Id);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _productRepo.Setup(r => r.GetByIdWithSizesAsync(product.Id, default))
            .ReturnsAsync(product);
        _comboRepo.Setup(r => r.Update(It.IsAny<Combo>()));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(command, default);

        result.Data.Should().NotBeNull();
        result.Data.Items.Count.Should().Be(1);
        result.Data.Items[0].ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryUpdate()
    {
        var combo = CreateTestCombo();
        var categoryId = Guid.NewGuid();
        var product = CreateTestProduct(categoryId);
        var command = ValidCommand(combo.Id, product.Id);

        _comboRepo.Setup(r => r.GetByIdAsync(combo.Id, default))
            .ReturnsAsync(combo);
        _productRepo.Setup(r => r.GetByIdWithSizesAsync(product.Id, default))
            .ReturnsAsync(product);
        _comboRepo.Setup(r => r.Update(It.IsAny<Combo>()));
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(command, default);

        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
