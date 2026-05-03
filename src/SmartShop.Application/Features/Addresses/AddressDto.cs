using SmartShop.Domain.Entities;

namespace SmartShop.Application.Features.Addresses;

public record AddressDto(
    Guid Id,
    string Label,
    string RecipientName,
    string Phone,
    string Street,
    string? Ward,
    string District,
    string City,
    bool IsDefault,
    DateTime CreatedAt)
{
    public static AddressDto From(UserAddress address) => new(
        address.Id,
        address.Label,
        address.RecipientName,
        address.Phone,
        address.Street,
        address.Ward,
        address.District,
        address.City,
        address.IsDefault,
        address.CreatedAt);
}
