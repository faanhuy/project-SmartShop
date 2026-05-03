using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities;

public class UserAddress : BaseAuditableEntity
{
    private UserAddress() { }

    public string UserId { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string? Ward { get; private set; }
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public static UserAddress Create(
        string userId,
        string label,
        string recipientName,
        string phone,
        string street,
        string? ward,
        string district,
        string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(district);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        return new UserAddress
        {
            UserId = userId,
            Label = label,
            RecipientName = recipientName,
            Phone = phone,
            Street = street,
            Ward = ward,
            District = district,
            City = city,
            IsDefault = false
        };
    }

    public void Update(
        string label,
        string recipientName,
        string phone,
        string street,
        string? ward,
        string district,
        string city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(district);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        Label = label;
        RecipientName = recipientName;
        Phone = phone;
        Street = street;
        Ward = ward;
        District = district;
        City = city;
    }

    public void SetAsDefault() => IsDefault = true;

    public void UnsetDefault() => IsDefault = false;
}
