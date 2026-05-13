using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Inventory.Commands.CreateSize;

public record CreateSizeCommand(
    SizeType Category,
    string Label,
    int DisplayOrder
) : IRequest<ApiResponse<SizeDto>>;
