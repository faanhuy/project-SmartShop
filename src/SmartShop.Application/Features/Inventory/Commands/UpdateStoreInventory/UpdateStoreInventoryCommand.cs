using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory.Queries.GetStoreInventory;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateStoreInventory;

public record UpdateStoreInventoryCommand(
    Guid StoreId,
    Guid ProductId,
    int Quantity) : IRequest<ApiResponse<StoreInventoryDto>>;
