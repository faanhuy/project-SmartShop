using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.SetStoreSizeInventory;

public record SetStoreSizeInventoryCommand(
    Guid StoreId,
    Guid ProductId,
    Guid SizeId,
    int Quantity) : IRequest<ApiResponse<bool>>;
