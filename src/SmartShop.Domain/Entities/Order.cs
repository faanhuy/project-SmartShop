using SmartShop.Domain.Common;
using SmartShop.Domain.Enums;

namespace SmartShop.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; private set; }
    public string ShippingAddress { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public decimal OriginalAmount { get; private set; }   // tổng trước giảm(= tổng các items)
    public decimal DiscountAmount { get; private set; }   // số tiền được giảm(= 0 nếu không dùng coupon)
    public string? CouponCode { get; private set; }       // mã đã dùng(nullable)

    public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.COD;
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; private set; }
    public string? VnpayTransactionId { get; private set; }

    public Guid? StoreId { get; private set; }

    public Guid? ShippingAddressId { get; private set; }
    public string? ShippingStreet { get; private set; }
    public int? ShippingWardId { get; private set; }
    public int? ShippingProvinceId { get; private set; }
    public Ward? ShippingWard { get; private set; }
    public Province? ShippingProvince { get; private set; }

    public User? User { get; private set; }
    public Store? Store { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(
        Guid userId,
        string shippingAddress,
        string? notes = null,
        string? shippingStreet = null,
        int? shippingWardId = null,
        int? shippingProvinceId = null,
        Guid? shippingAddressId = null)
    {
        return new Order
        {
            UserId = userId,
            ShippingAddress = shippingAddress,
            Notes = notes,
            ShippingStreet = shippingStreet,
            ShippingWardId = shippingWardId,
            ShippingProvinceId = shippingProvinceId,
            ShippingAddressId = shippingAddressId
        };
    }

    public void SetStoreId(Guid storeId)
    {
        if (storeId == Guid.Empty)
            throw new ArgumentException("StoreId không được để trống.", nameof(storeId));
        StoreId = storeId;
    }

    public void SetPaymentMethod(PaymentMethod method)
    {
        PaymentMethod = method;
    }

    public void MarkAsPaid(string transactionId, DateTime paidAt)
    {
        PaymentStatus = PaymentStatus.Paid;
        Status = OrderStatus.Confirmed;
        VnpayTransactionId = transactionId;
        PaidAt = paidAt;
    }

    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
    }

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        RecalculateTotal();
    }

    private void RecalculateTotal()
    {
        OriginalAmount = _items.Sum(i => i.UnitPrice * i.Quantity);
        TotalAmount = OriginalAmount - DiscountAmount;
    }

    public void UpdateStatus(OrderStatus status)
    {
        Status = status;
    }

    public void ApplyCoupon(string couponCode, decimal discountAmount)
    {
        CouponCode = couponCode;
        DiscountAmount = discountAmount;
        TotalAmount = OriginalAmount - DiscountAmount;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Không thể hủy đơn hàng đã giao.");

        Status = OrderStatus.Cancelled;
    }
}
