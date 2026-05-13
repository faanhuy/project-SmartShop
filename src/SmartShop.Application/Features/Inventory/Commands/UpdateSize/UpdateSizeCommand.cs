using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateSize;

public record UpdateSizeCommand(
    Guid Id,
    string Label,
    int DisplayOrder
) : IRequest<ApiResponse<SizeDto>>;
