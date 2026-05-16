using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Catalog.Queries.GetCatalog;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Catalog;

public class GetCatalogQueryHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IComboRepository> _comboRepo = new();

    private GetCatalogQueryHandler CreateHandler() =>
        new(_productRepo.Object, _comboRepo.Object);

    private static Product CreateTestProduct(string name = "Test Product")
    {
        return Product.Create(
            name,
            "Description",
            100m,
            Guid.NewGuid(),
            name.ToLower().Replace(" ", "-")
        );
    }

    private static Combo CreateTestCombo(
        decimal salePrice,
        DateTime? startsAt = null,
        DateTime? endsAt = null)
    {
        var combo = Combo.Create(
            "Test Combo",
            "Test Combo Title",
            "Description",
            "image.jpg",
            salePrice,
            startsAt ?? DateTime.UtcNow.AddDays(-1),
            endsAt ?? DateTime.UtcNow.AddDays(30)
        );
        return combo;
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyLists()
    {
        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo>());

        var result = await CreateHandler().Handle(query, default);

        result.Success.Should().BeTrue();
        result.Data.Products.Should().BeEmpty();
        result.Data.Combos.Should().BeEmpty();
        result.Data.TotalProducts.Should().Be(0);
        result.Data.TotalCombos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ActiveCombos_ReturnedInCatalog()
    {
        var combo = CreateTestCombo(salePrice: 99.99m);
        var product1 = CreateTestProduct("Product1");
        _ = product1; // price set at creation
        var comboItem = ComboItem.Create(combo.Id, product1.Id, product1.Name, null, null, 2, 50m);
        combo.AddItem(comboItem);

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        result.Data.Combos.Should().HaveCount(1);
        result.Data.Combos[0].Id.Should().Be(combo.Id);
        result.Data.Combos[0].Name.Should().Be(combo.Title);
        result.Data.Combos[0].ItemType.Should().Be("Combo");
    }

    [Fact]
    public async Task Handle_NoCombos_CatalogHasNoCombos()
    {
        var product = CreateTestProduct();
        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product> { product }, 1));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo>());

        var result = await CreateHandler().Handle(query, default);

        result.Data.Combos.Should().BeEmpty();
        result.Data.TotalCombos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ComboMapping_CalculatesDiscountPercent()
    {
        var combo = CreateTestCombo(salePrice: 80m);
        var product1 = CreateTestProduct("Product1");
        _ = product1; // price set at creation
        var product2 = CreateTestProduct("Product2");
        _ = product2; // price set at creation

        var item1 = ComboItem.Create(combo.Id, product1.Id, product1.Name, null, null, 2, 50m);
        var item2 = ComboItem.Create(combo.Id, product2.Id, product2.Name, null, null, 1, 75m);
        combo.AddItem(item1);
        combo.AddItem(item2);

        // Original = 50*2 + 75*1 = 175, Sale = 80
        // Discount = (175 - 80) / 175 * 100 = 54.3%

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        result.Data.Combos.Should().HaveCount(1);
        var comboCatalogItem = result.Data.Combos[0];
        comboCatalogItem.OriginalPrice.Should().Be(175m);
        comboCatalogItem.Price.Should().Be(80m);
        ((float)comboCatalogItem.DiscountPercent!.Value).Should().BeApproximately(54.3f, 0.1f);
    }

    [Fact]
    public async Task Handle_ComboSalePriceEqualOriginal_DiscountPercentNull()
    {
        var combo = CreateTestCombo(salePrice: 100m);
        var product = CreateTestProduct();

        var item = ComboItem.Create(combo.Id, product.Id, product.Name, null, null, 2, 50m);
        combo.AddItem(item);

        // Original = 50*2 = 100, Sale = 100, no discount

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        var comboCatalogItem = result.Data.Combos[0];
        comboCatalogItem.DiscountPercent.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CatalogItemType_ComboHasCorrectItemType()
    {
        var combo = CreateTestCombo(salePrice: 99.99m);
        var product = CreateTestProduct();
        var item = ComboItem.Create(combo.Id, product.Id, product.Name, null, null, 1, 50m);
        combo.AddItem(item);

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        result.Data.Combos[0].ItemType.Should().Be("Combo");
    }

    [Fact]
    public async Task Handle_ProductsAndCombos_BothReturned()
    {
        var product = CreateTestProduct("Product");
        var combo = CreateTestCombo(salePrice: 99.99m);
        var comboItem = ComboItem.Create(combo.Id, product.Id, product.Name, null, null, 1, 100m);
        combo.AddItem(comboItem);

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product> { product }, 1));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        result.Data.Products.Should().HaveCount(1);
        result.Data.Combos.Should().HaveCount(1);
        result.Data.TotalProducts.Should().Be(1);
        result.Data.TotalCombos.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ComboWithMultipleItems_ComboItemCountMatches()
    {
        var combo = CreateTestCombo(salePrice: 99.99m);
        var product1 = CreateTestProduct("Product1");
        var product2 = CreateTestProduct("Product2");
        var product3 = CreateTestProduct("Product3");

        combo.AddItem(ComboItem.Create(combo.Id, product1.Id, product1.Name, null, null, 1, 50m));
        combo.AddItem(ComboItem.Create(combo.Id, product2.Id, product2.Name, null, null, 2, 30m));
        combo.AddItem(ComboItem.Create(combo.Id, product3.Id, product3.Name, null, null, 1, 40m));

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product>(), 0));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo> { combo });

        var result = await CreateHandler().Handle(query, default);

        result.Data.Combos[0].ComboItemCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_InactiveProduct_NotReturnedInCatalog()
    {
        var activeProduct = CreateTestProduct("Active");
        var inactiveProduct = CreateTestProduct("Inactive");
        inactiveProduct.Deactivate();

        var query = new GetCatalogQuery(1, 10);

        _productRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, "newest", default))
            .ReturnsAsync((new List<Product> { activeProduct, inactiveProduct }, 2));
        _comboRepo.Setup(r => r.GetActiveAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Combo>());

        var result = await CreateHandler().Handle(query, default);

        result.Data.Products.Should().HaveCount(1);
        result.Data.Products[0].Name.Should().Be("Active");
    }
}
