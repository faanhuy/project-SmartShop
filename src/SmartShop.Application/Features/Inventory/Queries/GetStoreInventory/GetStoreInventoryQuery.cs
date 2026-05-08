using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Queries.GetStoreInventory;

public record GetStoreInventoryQuery(Guid StoreId) : IRequest<ApiResponse<List<StoreInventoryDto>>>;

public record StoreInventoryDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    int LowStockThreshold);
