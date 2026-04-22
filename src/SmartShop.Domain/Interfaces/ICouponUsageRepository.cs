using SmartShop.Domain.Entities;

namespace SmartShop.Domain.Interfaces;

public interface ICouponUsageRepository
{
    Task<CouponUsage?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<CouponUsage?> GetByCouponCodeAsync(string code, CancellationToken ct = default);
    Task<CouponUsage?> GetByCouponIdAsync(Guid couponId, CancellationToken ct = default);
    Task<CouponUsage?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task AddAsync(CouponUsage couponUsage, CancellationToken ct = default);
    void Update(CouponUsage couponUsage);
    void Delete(CouponUsage couponUsage);
}
