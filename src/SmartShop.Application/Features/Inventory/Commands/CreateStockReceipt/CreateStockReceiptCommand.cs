using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Commands.CreateStockReceipt;

public record CreateStockReceiptCommand(
    Guid StoreId,
    DateTime ReceiptDate,
    string? Notes,
    List<CreateStockReceiptItemRequest> Items
) : IRequest<ApiResponse<StockReceiptDto>>;
