using MediatR;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Orders.Commands.PlaceOrder;

public record PlaceOrderCommand(
    Guid UserId,
    string ShippingAddress,
    string? Notes,
    string? CouponCode,
    PaymentMethod PaymentMethod = PaymentMethod.COD) : IRequest<OrderDto>;
    