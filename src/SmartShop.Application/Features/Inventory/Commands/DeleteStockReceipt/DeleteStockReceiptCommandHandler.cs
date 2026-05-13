using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.DeleteStockReceipt;

public class DeleteStockReceiptCommandHandler(IStockReceiptRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteStockReceiptCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(DeleteStockReceiptCommand request, CancellationToken ct)
    {
        var receipt = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Phiếu nhập hàng với ID {request.Id} không tồn tại.");

        if (receipt.Status != ReceiptStatus.Pending)
            throw new InvalidOperationException($"Chỉ có thể xóa phiếu có trạng thái Pending. Trạng thái hiện tại: {receipt.Status}");

        repo.Delete(receipt);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { message = "Phiếu nhập hàng đã bị xóa." });
    }
}
