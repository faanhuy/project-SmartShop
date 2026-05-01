using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, OrderStatus? statusFilter = null, CancellationToken ct = default)
    {
        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        query = query.OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await context.Orders.AddAsync(order, ct);
    }

    public async Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetRevenueSummaryAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var query = context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.CreatedAt.Date >= from.Date
                        && o.CreatedAt.Date <= to.Date);

        var totalOrders = await query.CountAsync(ct);
        var totalRevenue = totalOrders > 0
            ? await query.SumAsync(o => o.TotalAmount, ct)
            : 0m;
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m;

        return (totalRevenue, totalOrders, averageOrderValue);
    }

    public async Task<(decimal PrevRevenue, int PrevOrders)> GetPrevPeriodSummaryAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var duration = to - from;
        var prevFrom = from - duration;
        var prevTo = from;

        var query = context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.CreatedAt.Date >= prevFrom.Date
                        && o.CreatedAt.Date < prevTo.Date);

        var prevOrders = await query.CountAsync(ct);
        var prevRevenue = prevOrders > 0
            ? await query.SumAsync(o => o.TotalAmount, ct)
            : 0m;

        return (prevRevenue, prevOrders);
    }

    public async Task<IEnumerable<(DateTime Date, decimal Revenue, int OrderCount)>> GetRevenueByDateAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.CreatedAt.Date >= from.Date
                        && o.CreatedAt.Date <= to.Date)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(r => r.Date)
            .ToListAsync(ct);

        return rows.Select(r => (r.Date, r.Revenue, r.OrderCount));
    }

    public async Task<IEnumerable<(Guid ProductId, string ProductName, int TotalQuantity, decimal TotalRevenue)>> GetTopProductsAsync(
        DateTime from, DateTime to, int limit, CancellationToken ct = default)
    {
        var rows = await context.Orders
            .Where(o => o.Status != OrderStatus.Cancelled
                        && o.CreatedAt.Date >= from.Date
                        && o.CreatedAt.Date <= to.Date)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.UnitPrice * i.Quantity)
            })
            .OrderByDescending(r => r.TotalRevenue)
            .Take(limit)
            .ToListAsync(ct);

        return rows.Select(r => (r.ProductId, r.ProductName, r.TotalQuantity, r.TotalRevenue));
    }

    public async Task<IEnumerable<(string Status, int Count)>> GetOrderStatusBreakdownAsync(
        CancellationToken ct = default)
    {
        var rows = await context.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Select(r => (r.Status.ToString(), r.Count));
    }

    public async Task<int> GetNewCustomersCountAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await context.Users
            .CountAsync(u => u.CreatedAt.Date >= from.Date && u.CreatedAt.Date <= to.Date, ct);
    }
}
