using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class WishlistItem : BaseAuditableEntity
{
    public string UserId { get; private set; } = string.Empty;
    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    private WishlistItem() { }

    public static WishlistItem Create(string userId, Guid productId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new WishlistItem
        {
            UserId = userId,
            ProductId = productId
        };
    }
}
