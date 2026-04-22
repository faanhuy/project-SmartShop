using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Order> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, OrderStatus? statusFilter = null, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);

    // Analytics
    Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetRevenueSummaryAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
    Task<(decimal PrevRevenue, int PrevOrders)> GetPrevPeriodSummaryAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<(DateTime Date, decimal Revenue, int OrderCount)>> GetRevenueByDateAsync(
        DateTime from, DateTime to, CancellationToken ct = default);
    Task<IEnumerable<(Guid ProductId, string ProductName, int TotalQuantity, decimal TotalRevenue)>> GetTopProductsAsync(
        DateTime from, DateTime to, int limit, CancellationToken ct = default);
    Task<IEnumerable<(string Status, int Count)>> GetOrderStatusBreakdownAsync(
        CancellationToken ct = default);
    Task<int> GetNewCustomersCountAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
