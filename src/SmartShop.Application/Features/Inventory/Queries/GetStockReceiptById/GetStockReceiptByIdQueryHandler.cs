using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Queries.GetStockReceiptById;

public class GetStockReceiptByIdQueryHandler(IStockReceiptRepository repo)
    : IRequestHandler<GetStockReceiptByIdQuery, ApiResponse<StockReceiptDetailDto>>
{
    public async Task<ApiResponse<StockReceiptDetailDto>> Handle(GetStockReceiptByIdQuery request, CancellationToken ct)
    {
        var receipt = await repo.GetByIdWithItemsAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Phiếu nhập hàng với ID {request.Id} không tồn tại.");

        var dto = StockReceiptDetailDto.From(receipt);
        return ApiResponse<StockReceiptDetailDto>.Ok(dto);
    }
}
