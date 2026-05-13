using SmartShop.Application.Common.Models;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Inventory;

public record SizeDto(Guid Id, string Category, string Label, int DisplayOrder, bool IsActive)
{
    public static SizeDto From(Size size)
    {
        return new SizeDto(
            size.Id,
            size.Category.ToString(),
            size.Label,
            size.DisplayOrder,
            size.IsActive
        );
    }
}
