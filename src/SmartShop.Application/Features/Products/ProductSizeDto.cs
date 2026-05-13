using SmartShop.Domain.Entities;

namespace SmartShop.Application.Features.Products;

public record ProductSizeDto(
    Guid Id,
    Guid ProductId,
    string SizeLabel,
    int DisplayOrder,
    bool IsActive,
    Guid? SizeId = null)
{
    public static ProductSizeDto From(ProductSize size) => new(
        size.Id,
        size.ProductId,
        size.SizeLabel,
        size.DisplayOrder,
        size.IsActive,
        size.SizeId);
}
