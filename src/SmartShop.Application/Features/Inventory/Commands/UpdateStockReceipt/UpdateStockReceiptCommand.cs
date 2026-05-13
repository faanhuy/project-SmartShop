using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory.Commands.CreateStockReceipt;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateStockReceipt;

public record UpdateStockReceiptCommand(
    Guid Id,
    DateTime ReceiptDate,
    string? Notes,
    List<CreateStockReceiptItemRequest> Items
) : IRequest<ApiResponse<StockReceiptDetailDto>>;
