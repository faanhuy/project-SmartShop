namespace SmartShop.Domain.Entities;

/// <summary>
/// Bảng cấu hình mở rộng: lưu số, text, boolean theo Key/Value.
/// DataType: "number" | "text" | "boolean"
/// </summary>
public class AppSetting
{
    public string Key         { get; private set; } = default!;
    public string Value       { get; private set; } = default!;
    public string DataType    { get; private set; } = "text";
    public string? Description { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AppSetting() { }

    public static AppSetting Create(string key, string value, string dataType = "text", string? description = null)
        => new()
        {
            Key         = key,
            Value       = value,
            DataType    = dataType,
            Description = description,
            UpdatedAt   = DateTime.UtcNow
        };

    public void SetValue(string value)
    {
        Value     = value;
        UpdatedAt = DateTime.UtcNow;
    }
}
