using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }
    public bool HasSizes { get; private set; }
    public SizeType? SizeType { get; private set; }

    public Category? Category { get; private set; }

    private readonly List<Review> _reviews = [];
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    private readonly List<ProductSize> _sizes = new();
    public IReadOnlyCollection<ProductSize> Sizes => _sizes.AsReadOnly();

    public static Product Create(string name, string description,
        decimal price, Guid categoryId, string slug,
        string? imageUrl = null, decimal? originalPrice = null,
        bool hasSizes = false, SizeType? sizeType = null)
    {
        return new Product
        {
            Name = name,
            Description = description,
            Price = price,
            OriginalPrice = originalPrice ?? price,
            CategoryId = categoryId,
            Slug = slug,
            ImageUrl = imageUrl,
            HasSizes = hasSizes,
            SizeType = sizeType
        };
    }

    public void Update(string name, string description, decimal price, string? imageUrl,
        decimal? originalPrice = null, bool hasSizes = false, SizeType? sizeType = null)
    {
        Name = name;
        Description = description;
        Price = price;
        OriginalPrice = originalPrice ?? OriginalPrice;
        ImageUrl = imageUrl;
        HasSizes = hasSizes;
        SizeType = sizeType;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
}
