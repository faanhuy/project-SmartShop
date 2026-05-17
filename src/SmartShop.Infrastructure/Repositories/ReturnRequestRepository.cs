using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class ReturnRequestRepository(ApplicationDbContext context) : IReturnRequestRepository
{
    public async Task<ReturnRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<ReturnRequest?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, ct);
    }

    public async Task<List<ReturnRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Order)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<ReturnRequest>> GetAllAsync(ReturnStatus? status, CancellationToken ct = default)
    {
        var query = context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Order)
            .Include(r => r.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(ReturnRequest returnRequest, CancellationToken ct = default)
    {
        await context.ReturnRequests.AddAsync(returnRequest, ct);
    }

    public void Update(ReturnRequest returnRequest)
    {
        context.ReturnRequests.Update(returnRequest);
    }
}
