using FluentAssertions;
using Moq;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Features.Reviews.Commands.AddReview;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.Reviews;

public class AddReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviewRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AddReviewCommandHandler CreateHandler() =>
        new(_reviewRepo.Object, _productRepo.Object, _uow.Object);

    private static Product CreateProduct() =>
        Product.Create("Product", "Desc", 100m, Guid.NewGuid(), "product-slug");

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);

        var act = () => CreateHandler().Handle(
            new AddReviewCommand(Guid.NewGuid(), productId, 5, "Great!"), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateReview_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = CreateProduct();
        var existingReview = Review.Create(userId, productId, 4, "Good");

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _reviewRepo.Setup(r => r.GetByUserAndProductAsync(userId, productId, default))
                   .ReturnsAsync(existingReview);

        var act = () => CreateHandler().Handle(
            new AddReviewCommand(userId, productId, 5, "Great!"), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsReviewDto()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _reviewRepo.Setup(r => r.GetByUserAndProductAsync(userId, productId, default))
                   .ReturnsAsync((Review?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new AddReviewCommand(userId, productId, 5, "Excellent product!"), default);

        result.Should().NotBeNull();
        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Excellent product!");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsReviewToRepository()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = CreateProduct();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _reviewRepo.Setup(r => r.GetByUserAndProductAsync(userId, productId, default))
                   .ReturnsAsync((Review?)null);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(
            new AddReviewCommand(userId, productId, 4, "Good"), default);

        _reviewRepo.Verify(r => r.AddAsync(It.IsAny<Review>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
