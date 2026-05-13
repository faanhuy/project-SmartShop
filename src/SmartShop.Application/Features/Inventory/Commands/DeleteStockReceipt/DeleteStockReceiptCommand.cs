using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.DeleteStockReceipt;

public record DeleteStockReceiptCommand(Guid Id) : IRequest<ApiResponse<object>>;
