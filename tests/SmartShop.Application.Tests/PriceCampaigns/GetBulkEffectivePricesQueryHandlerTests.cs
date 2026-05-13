using FluentAssertions;
using Moq;
using SmartShop.Application.Features.PriceCampaigns.Queries.GetBulkEffectivePrices;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using Xunit;

namespace SmartShop.Application.Tests.PriceCampaigns;

public class GetBulkEffectivePricesQueryHandlerTests
{
    private readonly Mock<IPriceCampaignRepository> _priceRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetBulkEffectivePricesQueryHandler CreateHandler() =>
        new(_priceRepo.Object, _productRepo.Object);

    private static Product MakeProduct(decimal price = 100m)
    {
        var p = Product.Create("Test Product", "Desc", price, Guid.NewGuid(), "test-slug");
        return p;
    }

    private static GetBulkEffectivePricesQuery MakeQuery(
        Guid storeId, Guid productId, Guid? sizeId = null, DateTime? at = null)
        => new(storeId, [new BulkEffectivePriceInput(productId, sizeId)], at);

    // ── 1. No active campaign → effective price = base price ──────────────

    [Fact]
    public async Task Handle_NoCampaign_EffectivePriceEqualsBasePrice()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = MakeProduct(100m);

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        var result = await CreateHandler().Handle(MakeQuery(storeId, productId), default);

        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].EffectivePrice.Should().Be(100m);
        result.Data![0].HasPromotion.Should().BeFalse();
    }

    // ── 2. 1 active campaign Coefficient → effective price computed ────────

    [Fact]
    public async Task Handle_CoefficientRule_EffectivePriceIsBaseTimesCoefficient()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = MakeProduct(200m);

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>
            {
                [(productId, null)] = ((int)PriceRuleType.Coefficient, 0.8m)
            });

        var result = await CreateHandler().Handle(MakeQuery(storeId, productId), default);

        result.Data![0].EffectivePrice.Should().Be(160m); // 200 * 0.8
        result.Data![0].HasPromotion.Should().BeTrue();
        result.Data![0].BasePrice.Should().Be(200m);
    }

    // ── 3. FixedPrice rule → effective price = fixed value ─────────────────

    [Fact]
    public async Task Handle_FixedPriceRule_EffectivePriceIsFixedValue()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = MakeProduct(300m);

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>
            {
                [(productId, null)] = ((int)PriceRuleType.FixedPrice, 250m)
            });

        var result = await CreateHandler().Handle(MakeQuery(storeId, productId), default);

        result.Data![0].EffectivePrice.Should().Be(250m);
        result.Data![0].HasPromotion.Should().BeTrue();
    }

    // ── 4. Multiple items — each gets correct effective price ──────────────

    [Fact]
    public async Task Handle_MultipleItems_EachGetsCorrectEffectivePrice()
    {
        var storeId = Guid.NewGuid();
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var product1 = MakeProduct(100m);
        var product2 = MakeProduct(200m);

        _productRepo.Setup(r => r.GetByIdAsync(productId1, default)).ReturnsAsync(product1);
        _productRepo.Setup(r => r.GetByIdAsync(productId2, default)).ReturnsAsync(product2);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>
            {
                [(productId1, null)] = ((int)PriceRuleType.Coefficient, 0.9m),
                // product2 has no rule
            });

        var query = new GetBulkEffectivePricesQuery(storeId, [
            new BulkEffectivePriceInput(productId1, null),
            new BulkEffectivePriceInput(productId2, null)
        ]);

        var result = await CreateHandler().Handle(query, default);

        result.Data.Should().HaveCount(2);

        var r1 = result.Data!.First(x => x.ProductId == productId1);
        r1.EffectivePrice.Should().Be(90m);
        r1.HasPromotion.Should().BeTrue();

        var r2 = result.Data!.First(x => x.ProductId == productId2);
        r2.EffectivePrice.Should().Be(200m);
        r2.HasPromotion.Should().BeFalse();
    }

    // ── 5. Product not found → base price defaults to 0 ────────────────────

    [Fact]
    public async Task Handle_ProductNotFound_BasePriceIsZero()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync((Product?)null);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        var result = await CreateHandler().Handle(MakeQuery(storeId, productId), default);

        result.Data![0].BasePrice.Should().Be(0m);
        result.Data![0].EffectivePrice.Should().Be(0m);
        result.Data![0].HasPromotion.Should().BeFalse();
    }

    // ── 6. Empty items list → empty result ─────────────────────────────────

    [Fact]
    public async Task Handle_EmptyItems_ReturnsEmptyList()
    {
        var storeId = Guid.NewGuid();

        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        var query = new GetBulkEffectivePricesQuery(storeId, []);
        var result = await CreateHandler().Handle(query, default);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    // ── 7. At timestamp is forwarded to repository ──────────────────────────

    [Fact]
    public async Task Handle_CustomAt_ForwardsTimestampToRepository()
    {
        var storeId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var at = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var product = MakeProduct(100m);

        _productRepo.Setup(r => r.GetByIdAsync(productId, default)).ReturnsAsync(product);
        _priceRepo
            .Setup(r => r.GetEffectivePriceItemsAsync(
                storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), at, default))
            .ReturnsAsync(new Dictionary<(Guid, Guid?), (int, decimal)>());

        await CreateHandler().Handle(MakeQuery(storeId, productId, at: at), default);

        _priceRepo.Verify(r => r.GetEffectivePriceItemsAsync(
            storeId, It.IsAny<IEnumerable<(Guid, Guid?)>>(), at, default), Times.Once);
    }
}
