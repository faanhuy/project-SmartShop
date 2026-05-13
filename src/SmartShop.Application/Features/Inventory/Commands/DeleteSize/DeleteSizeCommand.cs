using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.DeleteSize;

public record DeleteSizeCommand(Guid Id) : IRequest<ApiResponse<object>>;
