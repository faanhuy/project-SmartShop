namespace SmartShop.Application.Features.Orders;

public class OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal SubTotal { get; init; }
}

public class OrderDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal OriginalAmount { get; init; }
    public string ShippingAddress { get; init; } = string.Empty;
    public Guid? ShippingAddressId { get; init; }
    public string? ShippingStreet { get; init; }
    public int? ShippingWardId { get; init; }
    public int? ShippingProvinceId { get; init; }
    public string? ShippingWardName { get; init; }
    public string? ShippingProvinceName { get; init; }
    public string? Notes { get; init; }
    public string? CouponCode { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime? PaidAt { get; init; }
    public string? VnpayTransactionId { get; init; }
    public List<OrderItemDto> Items { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
