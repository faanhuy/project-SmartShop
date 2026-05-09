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

    // Structured geography FKs (Sprint 18B)
    public int? ProvinceId { get; private set; }
    public int? WardId { get; private set; }
    public Province? Province { get; private set; }
    public Ward? WardEntity { get; private set; }

    public static UserAddress Create(
        string userId,
        string label,
        string recipientName,
        string phone,
        string street,
        string? ward,
        string district,
        string city,
        int? provinceId = null,
        int? wardId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
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
            IsDefault = false,
            ProvinceId = provinceId,
            WardId = wardId
        };
    }

    public void Update(
        string label,
        string recipientName,
        string phone,
        string street,
        string? ward,
        string district,
        string city,
        int? provinceId = null,
        int? wardId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        Label = label;
        RecipientName = recipientName;
        Phone = phone;
        Street = street;
        Ward = ward;
        District = district;
        City = city;
        ProvinceId = provinceId;
        WardId = wardId;
    }

    public void SetAsDefault() => IsDefault = true;

    public void UnsetDefault() => IsDefault = false;
}
