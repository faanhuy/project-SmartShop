using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Addresses;

namespace SmartShop.Application.Features.Addresses.Commands.AddAddress;

public record AddAddressRequest(
    string Label,
    string RecipientName,
    string Phone,
    string Street,
    string? Ward,
    string District,
    string City,
    int? ProvinceId = null,
    int? WardId = null);

public record AddAddressCommand(
    string UserId,
    string Label,
    string RecipientName,
    string Phone,
    string Street,
    string? Ward,
    string District,
    string City,
    int? ProvinceId = null,
    int? WardId = null) : IRequest<ApiResponse<AddressDto>>;
