using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Addresses.Commands.SetDefaultAddress;

public record SetDefaultAddressCommand(Guid AddressId, string UserId) : IRequest<ApiResponse<bool>>;
