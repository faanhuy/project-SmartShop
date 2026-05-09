using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Application.Interfaces;
using SmartShop.Application.Products.Commands.UpdateProduct;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.Products;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    private UpdateProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _cache.Object);

    private static Product CreateProduct(Guid categoryId) =>
        Product.Create("Original Name", "Original Desc", 100m, categoryId, "original-slug");

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsUpdatedDto()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid());
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new UpdateProductCommand(productId, "Updated Name", "Updated Desc", 200m, null);
        var result = await CreateHandler().Handle(command, default);

        result.Should().BeOfType<ProductDto>();
        result.Name.Should().Be("Updated Name");
        result.Price.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(
            new UpdateProductCommand(productId, "N", "D", 10m, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Success_InvalidatesBothCaches()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid());
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(
            new UpdateProductCommand(productId, "N", "D", 10m, null), default);

        _cache.Verify(c => c.RemoveAsync($"products:id:{productId}", default), Times.Once);
        _cache.Verify(c => c.RemoveByPrefixAsync("products:list:", default), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_CallsRepositoryUpdate()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid());
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(
            new UpdateProductCommand(productId, "N", "D", 10m, null), default);

        _productRepo.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
    }
}
