using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.CompleteStockReceipt;

public record CompleteStockReceiptCommand(Guid Id) : IRequest<ApiResponse<StockReceiptDto>>;
