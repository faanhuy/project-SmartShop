using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.CompleteStockReceipt;

public class CompleteStockReceiptCommandHandler(
    IStockReceiptRepository receiptRepo,
    IStoreSizeInventoryRepository storeSizeInventoryRepo,
    IStoreInventoryRepository storeInventoryRepo,
    IProductSizeRepository productSizeRepo,
    IUnitOfWork uow)
    : IRequestHandler<CompleteStockReceiptCommand, ApiResponse<StockReceiptDto>>
{
    public async Task<ApiResponse<StockReceiptDto>> Handle(CompleteStockReceiptCommand request, CancellationToken ct)
    {
        var receipt = await receiptRepo.GetByIdWithItemsAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Phiếu nhập hàng với ID {request.Id} không tồn tại.");

        if (receipt.Status != ReceiptStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể hoàn thành phiếu có trạng thái Pending. Trạng thái hiện tại: {receipt.Status}");

        var sizedItems = receipt.Items
            .Where(item => item.SizeId.HasValue)
            .GroupBy(item => new { item.ProductId, SizeId = item.SizeId!.Value })
            .Select(group => new
            {
                group.Key.ProductId,
                group.Key.SizeId,
                Quantity = group.Sum(item => item.Quantity)
            });

        foreach (var item in sizedItems)
        {
            var productSizeId = await ResolveProductSizeIdAsync(
                item.ProductId, item.SizeId, ct);

            var sizeInventory = await storeSizeInventoryRepo.GetAsync(
                receipt.StoreId, item.ProductId, productSizeId, ct);

            if (sizeInventory is null)
            {
                sizeInventory = Domain.Entities.StoreSizeInventory.Create(
                    receipt.StoreId, item.ProductId, productSizeId, item.Quantity);
                await storeSizeInventoryRepo.AddAsync(sizeInventory, ct);
            }
            else
            {
                sizeInventory.SetQuantity(sizeInventory.Quantity + item.Quantity);
                storeSizeInventoryRepo.Update(sizeInventory);
            }
        }

        var productQuantities = receipt.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            });

        foreach (var item in productQuantities)
        {
            await IncreaseStoreInventoryAsync(receipt.StoreId, item.ProductId, item.Quantity, ct);
        }

        receipt.Complete();
        receiptRepo.Update(receipt);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<StockReceiptDto>.Ok(StockReceiptDto.From(receipt));
    }

    private async Task<Guid> ResolveProductSizeIdAsync(
        Guid productId, Guid masterSizeId, CancellationToken ct)
    {
        var productSizes = await productSizeRepo.GetByProductIdAsync(productId, ct);
        var productSize = productSizes.FirstOrDefault(ps => ps.SizeId == masterSizeId);

        if (productSize is null)
        {
            throw new InvalidOperationException(
                $"Sản phẩm {productId} chưa được gắn kích thước master {masterSizeId}.");
        }

        return productSize.Id;
    }

    private async Task IncreaseStoreInventoryAsync(
        Guid storeId, Guid productId, int quantity, CancellationToken ct)
    {
        var inventory = await storeInventoryRepo.GetAsync(storeId, productId, ct);

        if (inventory is null)
        {
            inventory = Domain.Entities.StoreInventory.Create(storeId, productId, quantity);
            await storeInventoryRepo.AddAsync(inventory, ct);
            return;
        }

        inventory.SetQuantity(inventory.Quantity + quantity);
        storeInventoryRepo.Update(inventory);
    }
}
