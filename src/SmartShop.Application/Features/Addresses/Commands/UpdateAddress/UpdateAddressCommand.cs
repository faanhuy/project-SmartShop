using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Addresses;

namespace SmartShop.Application.Features.Addresses.Commands.UpdateAddress;

public record UpdateAddressRequest(
    string Label,
    string RecipientName,
    string Phone,
    string Street,
    string? Ward,
    string District,
    string City);

public record UpdateAddressCommand(
    Guid AddressId,
    string UserId,
    string Label,
    string RecipientName,
    string Phone,
    string Street,
    string? Ward,
    string District,
    string City) : IRequest<ApiResponse<AddressDto>>;
