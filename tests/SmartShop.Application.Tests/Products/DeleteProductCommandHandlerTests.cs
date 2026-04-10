using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
using Xunit;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Interfaces;
using SmartShop.Application.Products.Commands.DeleteProduct;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    private DeleteProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _uow.Object, _cache.Object);

    [Fact]
    public async Task Handle_ExistingProduct_DeactivatesProduct()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 5, Guid.NewGuid(), "product-slug");
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        Product? captured = null;
        _productRepo.Setup(r => r.Update(It.IsAny<Product>()))
                    .Callback<Product>(p => captured = p);

        await CreateHandler().Handle(new DeleteProductCommand(productId), default);

        captured.Should().NotBeNull();
        captured!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(new DeleteProductCommand(productId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExistingProduct_RemovesFromCache()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 5, Guid.NewGuid(), "product-slug");
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteProductCommand(productId), default);

        _cache.Verify(c => c.RemoveAsync($"products:id:{productId}", default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingProduct_InvalidatesListCache()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 5, Guid.NewGuid(), "product-slug");
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteProductCommand(productId), default);

        _cache.Verify(c => c.RemoveByPrefixAsync("products:list:", default), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingProduct_CallsRepositoryUpdate()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, 5, Guid.NewGuid(), "product-slug");
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteProductCommand(productId), default);

        _productRepo.Verify(r => r.Update(It.IsAny<Product>()), Times.Once);
    }
}
