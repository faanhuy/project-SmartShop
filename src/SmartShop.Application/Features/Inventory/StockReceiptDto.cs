using SmartShop.Application.Common.Models;
using SmartShop.Domain.Entities;

namespace SmartShop.Application.Features.Inventory;

public record StockReceiptItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid? SizeId,
    string? SizeLabel,
    int Quantity,
    string? Notes
)
{
    public static StockReceiptItemDto From(StockReceiptItem item)
    {
        return new StockReceiptItemDto(
            item.Id,
            item.ProductId,
            item.Product?.Name ?? "Unknown",
            item.SizeId,
            item.Size?.Label,
            item.Quantity,
            item.Notes
        );
    }
}

public record StockReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid StoreId,
    DateTime ReceiptDate,
    string? Notes,
    string Status,
    DateTime CreatedAt
)
{
    public static StockReceiptDto From(StockReceipt receipt)
    {
        return new StockReceiptDto(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.StoreId,
            receipt.ReceiptDate,
            receipt.Notes,
            receipt.Status.ToString(),
            receipt.CreatedAt
        );
    }
}

public record StockReceiptDetailDto(
    Guid Id,
    string ReceiptNumber,
    Guid StoreId,
    DateTime ReceiptDate,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    List<StockReceiptItemDto> Items
)
{
    public static StockReceiptDetailDto From(StockReceipt receipt)
    {
        var items = receipt.Items
            .Select(StockReceiptItemDto.From)
            .ToList();

        return new StockReceiptDetailDto(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.StoreId,
            receipt.ReceiptDate,
            receipt.Notes,
            receipt.Status.ToString(),
            receipt.CreatedAt,
            items
        );
    }
}
