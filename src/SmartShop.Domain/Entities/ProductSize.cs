using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class ProductSize : BaseAuditableEntity
{
    public Guid ProductId { get; private set; }
    public string SizeLabel { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? SizeId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Size? Size { get; private set; }

    private ProductSize() { }

    public static ProductSize Create(Guid productId, string sizeLabel, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sizeLabel);
        return new ProductSize
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SizeLabel = sizeLabel,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public static ProductSize CreateFromMaster(Guid productId, Guid sizeId, string sizeLabel, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sizeLabel);
        return new ProductSize
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            SizeId = sizeId,
            SizeLabel = sizeLabel,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(string sizeLabel, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sizeLabel);
        SizeLabel = sizeLabel;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
}
