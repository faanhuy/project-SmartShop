using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.CancelStockReceipt;

public record CancelStockReceiptCommand(Guid Id) : IRequest<ApiResponse<StockReceiptDto>>;
