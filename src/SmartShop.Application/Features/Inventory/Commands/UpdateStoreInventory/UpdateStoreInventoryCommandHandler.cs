using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory.Queries.GetStoreInventory;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateStoreInventory;

public class UpdateStoreInventoryCommandHandler(
    IStoreRepository storeRepository,
    IStoreInventoryRepository storeInventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStoreInventoryCommand, ApiResponse<StoreInventoryDto>>
{
    public async Task<ApiResponse<StoreInventoryDto>> Handle(
        UpdateStoreInventoryCommand request, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        var inventory = await storeInventoryRepository.GetByStoreAndProductAsync(
            request.StoreId, request.ProductId, cancellationToken);

        if (inventory is null)
        {
            inventory = StoreInventory.Create(request.StoreId, request.ProductId, request.Quantity);
            await storeInventoryRepository.AddAsync(inventory, cancellationToken);
        }
        else
        {
            inventory.SetQuantity(request.Quantity);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var productName = inventory.Product?.Name ?? string.Empty;
        var dto = new StoreInventoryDto(
            inventory.ProductId,
            productName,
            inventory.Quantity,
            inventory.LowStockThreshold);

        return ApiResponse<StoreInventoryDto>.Ok(dto);
    }
}
