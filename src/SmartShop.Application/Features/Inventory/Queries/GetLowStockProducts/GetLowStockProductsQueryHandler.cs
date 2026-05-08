using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Queries.GetLowStockProducts;

public class GetLowStockProductsQueryHandler(
    IStoreRepository storeRepository,
    IStoreInventoryRepository storeInventoryRepository)
    : IRequestHandler<GetLowStockProductsQuery, ApiResponse<List<LowStockProductDto>>>
{
    public async Task<ApiResponse<List<LowStockProductDto>>> Handle(
        GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        _ = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        var inventories = await storeInventoryRepository.GetByStoreIdAsync(request.StoreId, cancellationToken);

        var lowStock = inventories
            .Where(i => i.Quantity <= i.LowStockThreshold)
            .Select(i => new LowStockProductDto(
                i.ProductId,
                i.Product?.Name ?? string.Empty,
                i.Quantity,
                i.LowStockThreshold))
            .ToList();

        return ApiResponse<List<LowStockProductDto>>.Ok(lowStock);
    }
}
