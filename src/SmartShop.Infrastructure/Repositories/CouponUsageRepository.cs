using Microsoft.EntityFrameworkCore;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Data;

namespace SmartShop.Infrastructure.Repositories;

public class CouponUsageRepository : ICouponUsageRepository
{
    private readonly ApplicationDbContext _context;
    public CouponUsageRepository(ApplicationDbContext context) => _context = context;

    public async Task AddAsync(CouponUsage usage, CancellationToken ct = default)
        => await _context.CouponUsages.AddAsync(usage, ct);

    public async Task<CouponUsage?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.CouponUsages.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task<CouponUsage?> GetByCouponCodeAsync(string code, CancellationToken ct = default)
        => await _context.CouponUsages
            .Include(u => u.Coupon)
            .FirstOrDefaultAsync(u => u.Coupon!.Code == code, ct);

    public async Task<CouponUsage?> GetByCouponIdAsync(Guid couponId, CancellationToken ct = default)
        => await _context.CouponUsages.FirstOrDefaultAsync(u => u.CouponId == couponId, ct);

    public async Task<CouponUsage?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => await _context.CouponUsages.FirstOrDefaultAsync(u => u.OrderId == orderId, ct);

    public void Update(CouponUsage usage) => _context.CouponUsages.Update(usage);

    public void Delete(CouponUsage usage) => _context.CouponUsages.Remove(usage);
}
