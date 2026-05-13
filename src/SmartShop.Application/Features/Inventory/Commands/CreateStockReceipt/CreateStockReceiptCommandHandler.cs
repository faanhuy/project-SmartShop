using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.CreateStockReceipt;

public class CreateStockReceiptCommandHandler(
    IStockReceiptRepository receiptRepo,
    IProductRepository productRepo,
    ISizeRepository sizeRepo,
    IUnitOfWork uow)
    : IRequestHandler<CreateStockReceiptCommand, ApiResponse<StockReceiptDto>>
{
    public async Task<ApiResponse<StockReceiptDto>> Handle(CreateStockReceiptCommand request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Items danh sách không được để trống.", nameof(request.Items));

        // Validate items
        foreach (var item in request.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId, ct)
                ?? throw new InvalidOperationException($"Sản phẩm với ID {item.ProductId} không tồn tại.");

            // If product has sizes, SizeId is required
            if (product.HasSizes && item.SizeId == null)
                throw new InvalidOperationException($"Sản phẩm '{product.Name}' yêu cầu chọn kích thước.");

            // If SizeId is provided, validate it exists
            if (item.SizeId.HasValue)
            {
                var size = await sizeRepo.GetByIdAsync(item.SizeId.Value, ct)
                    ?? throw new InvalidOperationException($"Kích thước với ID {item.SizeId} không tồn tại.");
            }
        }

        // Generate receipt number
        var receiptNumber = await receiptRepo.GenerateReceiptNumberAsync(ct);

        // Create receipt
        var receipt = StockReceipt.Create(request.StoreId, request.ReceiptDate, request.Notes, receiptNumber);

        // Add items
        foreach (var item in request.Items)
        {
            var receiptItem = StockReceiptItem.Create(
                receipt.Id,
                item.ProductId,
                item.SizeId,
                item.Quantity,
                item.Notes
            );
            receipt.AddItem(receiptItem);
        }

        await receiptRepo.AddAsync(receipt, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<StockReceiptDto>.Ok(StockReceiptDto.From(receipt));
    }
}
