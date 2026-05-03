using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    public static Category Create(string name, string slug, string? description = null)
    {
        return new Category
        {
            Name = name,
            Slug = slug,
            Description = description
        };
    }

    public void Update(string name, string slug, string? description, string? imageUrl)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate() => IsActive = false;
}
