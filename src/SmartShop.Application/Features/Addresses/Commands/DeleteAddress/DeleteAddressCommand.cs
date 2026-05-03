using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Addresses.Commands.DeleteAddress;

public record DeleteAddressCommand(Guid AddressId, string UserId) : IRequest<ApiResponse<bool>>;
