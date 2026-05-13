using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.SetStoreSizeInventory;

public class SetStoreSizeInventoryCommandHandler(
    IStoreRepository storeRepository,
    IProductSizeRepository productSizeRepository,
    IStoreSizeInventoryRepository storeSizeInventoryRepository,
    IStoreInventoryRepository storeInventoryRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetStoreSizeInventoryCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        SetStoreSizeInventoryCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 0)
            throw new ArgumentException("Số lượng tồn kho không được âm.");

        _ = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        _ = await productSizeRepository.GetByIdAsync(request.SizeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductSize), request.SizeId);

        var inventory = await storeSizeInventoryRepository.GetAsync(
            request.StoreId, request.ProductId, request.SizeId, cancellationToken);
        var previousQuantity = inventory?.Quantity ?? 0;

        if (inventory is null)
        {
            inventory = StoreSizeInventory.Create(
                request.StoreId, request.ProductId, request.SizeId, request.Quantity);
            await storeSizeInventoryRepository.AddAsync(inventory, cancellationToken);
        }
        else
        {
            inventory.SetQuantity(request.Quantity);
            storeSizeInventoryRepository.Update(inventory);
        }

        await AdjustStoreInventoryAsync(
            request.StoreId,
            request.ProductId,
            request.Quantity - previousQuantity,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true);
    }

    private async Task AdjustStoreInventoryAsync(
        Guid storeId, Guid productId, int delta, CancellationToken cancellationToken)
    {
        if (delta == 0)
            return;

        var inventory = await storeInventoryRepository.GetAsync(storeId, productId, cancellationToken);

        if (inventory is null)
        {
            inventory = StoreInventory.Create(storeId, productId, Math.Max(0, delta));
            await storeInventoryRepository.AddAsync(inventory, cancellationToken);
            return;
        }

        inventory.SetQuantity(inventory.Quantity + delta);
        storeInventoryRepository.Update(inventory);
    }
}
