using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Queries.GetStockReceipts;

public class GetStockReceiptsQueryHandler(IStockReceiptRepository repo)
    : IRequestHandler<GetStockReceiptsQuery, ApiResponse<PagedResult<StockReceiptDto>>>
{
    public async Task<ApiResponse<PagedResult<StockReceiptDto>>> Handle(GetStockReceiptsQuery request, CancellationToken ct)
    {
        var (receipts, total) = await repo.GetPagedAsync(
            request.StoreId,
            request.Page,
            request.PageSize,
            request.Status,
            ct
        );

        var dtos = receipts.Select(StockReceiptDto.From).ToList();
        var result = new PagedResult<StockReceiptDto>(dtos, request.Page, request.PageSize, total);

        return ApiResponse<PagedResult<StockReceiptDto>>.Ok(result);
    }
}
