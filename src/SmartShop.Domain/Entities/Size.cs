using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class Size : BaseAuditableEntity
{
    public SizeType Category { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    private Size() { }

    public static Size Create(SizeType category, string label, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return new Size
        {
            Id = Guid.NewGuid(),
            Category = category,
            Label = label,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public void Update(string label, int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
}
