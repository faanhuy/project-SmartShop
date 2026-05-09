using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Application.Interfaces;
using SmartShop.Application.Products.Commands.CreateProduct;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Products;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICacheService> _cache = new();

    private CreateProductCommandHandler CreateHandler() =>
        new(_productRepo.Object, _categoryRepo.Object, _uow.Object, _cache.Object);

    private static CreateProductCommand ValidCommand(Guid categoryId) =>
        new("Test Product", "Description", 99.99m, categoryId, "test-product");

    [Fact]
    public async Task Handle_ValidRequest_ReturnsProductDto()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "electronics");
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetBySlugAsync("test-product", default)).ReturnsAsync((Product?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(categoryId), default);

        result.Should().BeOfType<ProductDto>();
        result.Name.Should().Be("Test Product");
        result.Slug.Should().Be("test-product");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync((Category?)null);

        var act = () => CreateHandler().Handle(ValidCommand(categoryId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsConflictException()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "electronics");
        var existingProduct = Product.Create("Existing", "Desc", 50m, categoryId, "test-product");
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetBySlugAsync("test-product", default)).ReturnsAsync(existingProduct);

        var act = () => CreateHandler().Handle(ValidCommand(categoryId), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_InvalidatesListCachePrefix()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "electronics");
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetBySlugAsync("test-product", default)).ReturnsAsync((Product?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(categoryId), default);

        _cache.Verify(c => c.RemoveByPrefixAsync("products:list:", default), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsProductToRepository()
    {
        var categoryId = Guid.NewGuid();
        var category = Category.Create("Electronics", "electronics");
        _categoryRepo.Setup(r => r.GetByIdAsync(categoryId, default)).ReturnsAsync(category);
        _productRepo.Setup(r => r.GetBySlugAsync("test-product", default)).ReturnsAsync((Product?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(categoryId), default);

        _productRepo.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
    }
}
