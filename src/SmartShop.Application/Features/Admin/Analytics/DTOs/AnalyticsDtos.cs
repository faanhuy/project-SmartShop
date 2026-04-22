namespace SmartShop.Application.Features.Admin.Analytics.DTOs;

public record RevenueSummaryDto(
    decimal TotalRevenue,
    int TotalOrders,
    int NewCustomers,
    decimal AverageOrderValue,
    decimal RevenueGrowthPercent);

public record RevenueByDateDto(string Date, decimal Revenue, int OrderCount);

public record TopProductDto(Guid ProductId, string ProductName, int TotalQuantity, decimal TotalRevenue);

public record OrderStatusBreakdownDto(string Status, int Count);
