using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Inventory.Queries.GetLowStockProducts;

public record GetLowStockProductsQuery(Guid StoreId) : IRequest<ApiResponse<List<LowStockProductDto>>>;

public record LowStockProductDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    int LowStockThreshold);
