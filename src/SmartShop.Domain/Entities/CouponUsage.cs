using SmartShop.Domain.Common;

namespace SmartShop.Domain.Entities
{

    public class CouponUsage : BaseAuditableEntity
    {
        public Guid UserId { get; private set; }
        public Guid OrderId { get; private set; }
        public Guid CouponId { get; private set; }

        private CouponUsage() { }

        public static CouponUsage Create(Guid userId, Guid orderId, Guid couponId)
        {
            return new CouponUsage
            {
                UserId = userId,
                OrderId = orderId,
                CouponId = couponId
            };
        }
        public User? User { get; private set; }
        public Coupon? Coupon { get; private set; }
        public Order? Order { get; private set; }
    }
}
