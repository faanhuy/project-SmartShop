using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Queries.GetStockReceiptById;

public record GetStockReceiptByIdQuery(Guid Id) : IRequest<ApiResponse<StockReceiptDetailDto>>;
