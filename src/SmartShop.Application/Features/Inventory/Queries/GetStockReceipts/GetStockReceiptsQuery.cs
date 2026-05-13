using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Inventory.Queries.GetStockReceipts;

public record GetStockReceiptsQuery(
    Guid StoreId,
    int Page = 1,
    int PageSize = 10,
    ReceiptStatus? Status = null
) : IRequest<ApiResponse<PagedResult<StockReceiptDto>>>;

public record PagedResult<T>(
    IEnumerable<T> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
