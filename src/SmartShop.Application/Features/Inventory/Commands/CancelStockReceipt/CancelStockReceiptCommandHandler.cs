using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.CancelStockReceipt;

public class CancelStockReceiptCommandHandler(
    IStockReceiptRepository receiptRepo,
    IStoreSizeInventoryRepository storeSizeInventoryRepo,
    IStoreInventoryRepository storeInventoryRepo,
    IProductSizeRepository productSizeRepo,
    IUnitOfWork uow)
    : IRequestHandler<CancelStockReceiptCommand, ApiResponse<StockReceiptDto>>
{
    public async Task<ApiResponse<StockReceiptDto>> Handle(CancelStockReceiptCommand request, CancellationToken ct)
    {
        var receipt = await receiptRepo.GetByIdWithItemsAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Phiếu nhập hàng với ID {request.Id} không tồn tại.");

        if (receipt.Status == ReceiptStatus.Cancelled)
            throw new InvalidOperationException("Phiếu nhập hàng đã được hủy trước đó.");

        if (receipt.Status == ReceiptStatus.Completed)
        {
            foreach (var item in receipt.Items)
            {
                if (item.SizeId.HasValue)
                {
                    var productSizeId = await ResolveProductSizeIdAsync(
                        item.ProductId, item.SizeId.Value, ct);

                    var sizeInventory = await storeSizeInventoryRepo.GetAsync(
                        receipt.StoreId, item.ProductId, productSizeId, ct);

                    if (sizeInventory is not null)
                    {
                        sizeInventory.SetQuantity(sizeInventory.Quantity - item.Quantity);
                        storeSizeInventoryRepo.Update(sizeInventory);
                    }
                }

                await DecreaseStoreInventoryAsync(receipt.StoreId, item.ProductId, item.Quantity, ct);
            }
        }

        receipt.Cancel();
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

    private async Task DecreaseStoreInventoryAsync(
        Guid storeId, Guid productId, int quantity, CancellationToken ct)
    {
        var inventory = await storeInventoryRepo.GetAsync(storeId, productId, ct);

        if (inventory is null)
            return;

        inventory.SetQuantity(inventory.Quantity - quantity);
        storeInventoryRepo.Update(inventory);
    }
}
