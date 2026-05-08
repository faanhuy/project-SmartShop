using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Queries.GetStoreInventory;

public class GetStoreInventoryQueryHandler(
    IStoreRepository storeRepository,
    IStoreInventoryRepository storeInventoryRepository)
    : IRequestHandler<GetStoreInventoryQuery, ApiResponse<List<StoreInventoryDto>>>
{
    public async Task<ApiResponse<List<StoreInventoryDto>>> Handle(
        GetStoreInventoryQuery request, CancellationToken cancellationToken)
    {
        _ = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        var inventories = await storeInventoryRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);

        var dtos = inventories.Select(i => new StoreInventoryDto(
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Quantity,
            i.LowStockThreshold)).ToList();

        return ApiResponse<List<StoreInventoryDto>>.Ok(dtos);
    }
}
