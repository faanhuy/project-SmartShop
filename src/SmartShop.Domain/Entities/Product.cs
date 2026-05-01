using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public decimal OriginalPrice { get; private set; }
    public int Stock { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    private readonly List<Review> _reviews = [];
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    public static Product Create(string name, string description,
        decimal price, int stock, Guid categoryId, string slug,
        string? imageUrl = null, decimal? originalPrice = null)
    {
        return new Product
        {
            Name = name,
            Description = description,
            Price = price,
            OriginalPrice = originalPrice ?? price,
            Stock = stock,
            CategoryId = categoryId,
            Slug = slug,
            ImageUrl = imageUrl
        };
    }

    public void Update(string name, string description, decimal price, string? imageUrl,
        decimal? originalPrice = null)
    {
        Name = name;
        Description = description;
        Price = price;
        OriginalPrice = originalPrice ?? OriginalPrice;
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity > Stock)
            throw new InvalidOperationException("Không đủ tồn kho.");
        Stock -= quantity;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}
