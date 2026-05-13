using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateStockReceipt;

public class UpdateStockReceiptCommandHandler(
    IStockReceiptRepository receiptRepo,
    IProductRepository productRepo,
    ISizeRepository sizeRepo,
    IUnitOfWork uow)
    : IRequestHandler<UpdateStockReceiptCommand, ApiResponse<StockReceiptDetailDto>>
{
    public async Task<ApiResponse<StockReceiptDetailDto>> Handle(UpdateStockReceiptCommand request, CancellationToken ct)
    {
        var receipt = await receiptRepo.GetByIdWithItemsAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Phiếu nhập hàng với ID {request.Id} không tồn tại.");

        if (receipt.Status != ReceiptStatus.Pending)
            throw new InvalidOperationException("Chỉ có thể chỉnh sửa phiếu có trạng thái Chờ xử lý.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Danh sách sản phẩm không được để trống.", nameof(request.Items));

        // Validate items
        foreach (var item in request.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId, ct)
                ?? throw new InvalidOperationException($"Sản phẩm với ID {item.ProductId} không tồn tại.");

            if (product.HasSizes && item.SizeId == null)
                throw new InvalidOperationException($"Sản phẩm '{product.Name}' yêu cầu chọn kích thước.");

            if (item.SizeId.HasValue)
            {
                _ = await sizeRepo.GetByIdAsync(item.SizeId.Value, ct)
                    ?? throw new InvalidOperationException($"Kích thước với ID {item.SizeId} không tồn tại.");
            }
        }

        // Replace all items
        receipt.ClearItems();
        receipt.UpdateNotes(request.Notes);
        receipt.UpdateReceiptDate(request.ReceiptDate);

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

        receiptRepo.Update(receipt);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<StockReceiptDetailDto>.Ok(StockReceiptDetailDto.From(receipt));
    }
}
