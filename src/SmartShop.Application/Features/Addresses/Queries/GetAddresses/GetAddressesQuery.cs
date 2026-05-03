using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Addresses;

namespace SmartShop.Application.Features.Addresses.Queries.GetAddresses;

public record GetAddressesQuery(string UserId) : IRequest<ApiResponse<List<AddressDto>>>;
