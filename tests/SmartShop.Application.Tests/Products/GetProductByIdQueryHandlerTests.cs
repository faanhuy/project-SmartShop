using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using Xunit;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.DTOs;
using SmartShop.Application.Products.Queries.GetProductById;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Products;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<ICacheService> _cache = new();

    private GetProductByIdQueryHandler CreateHandler() =>
        new(_productRepo.Object, _cache.Object);

    private static ProductDto MakeDto(Guid id) =>
        new(id, "Cached Product", "Desc", 50m, 50m, "cached-slug", null, true, Guid.NewGuid(), DateTime.UtcNow);

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedDtoWithoutRepoCall()
    {
        var productId = Guid.NewGuid();
        var cachedDto = MakeDto(productId);
        _cache.Setup(c => c.GetAsync<ProductDto>($"products:id:{productId}", default))
              .ReturnsAsync(cachedDto);

        var result = await CreateHandler().Handle(new GetProductByIdQuery(productId), default);

        result.Should().Be(cachedDto);
        _productRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_ProductFound_ReturnsDto()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, Guid.NewGuid(), "product-slug");
        _cache.Setup(c => c.GetAsync<ProductDto>($"products:id:{productId}", default))
              .ReturnsAsync((ProductDto?)null);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        var result = await CreateHandler().Handle(new GetProductByIdQuery(productId), default);

        result.Name.Should().Be("Product");
    }

    [Fact]
    public async Task Handle_CacheMiss_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync<ProductDto>($"products:id:{productId}", default))
              .ReturnsAsync((ProductDto?)null);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(new GetProductByIdQuery(productId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CacheMiss_ProductFound_SetsCacheWithTenMinuteExpiry()
    {
        var productId = Guid.NewGuid();
        var product = Product.Create("Product", "Desc", 50m, Guid.NewGuid(), "product-slug");
        _cache.Setup(c => c.GetAsync<ProductDto>($"products:id:{productId}", default))
              .ReturnsAsync((ProductDto?)null);
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);

        await CreateHandler().Handle(new GetProductByIdQuery(productId), default);

        _cache.Verify(c => c.SetAsync(
            $"products:id:{productId}",
            It.IsAny<ProductDto>(),
            TimeSpan.FromMinutes(10),
            default), Times.Once);
    }
}
