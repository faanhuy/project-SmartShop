namespace SmartShop.Application.Features.Inventory.Commands.CreateStockReceipt;

public record CreateStockReceiptItemRequest(
    Guid ProductId,
    Guid? SizeId,
    int Quantity,
    string? Notes = null
);
