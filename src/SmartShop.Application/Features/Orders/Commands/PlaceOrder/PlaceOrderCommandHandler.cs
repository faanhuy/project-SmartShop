using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;
using SmartShop.Domain.Events;

namespace SmartShop.Application.Features.Orders.Commands.PlaceOrder;

public class PlaceOrderCommandHandler(
    ICartRepository cartRepository,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    ICouponRepository couponRepository,
    ICouponUsageRepository couponUsageRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator) : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await cartRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Cart", request.UserId);

        if (!cart.Items.Any())
            throw new ConflictException("Giỏ hàng đang trống.");

        // Validate stock for all items
        foreach (var item in cart.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new NotFoundException("Product", item.ProductId);

            if (!product.IsActive)
                throw new ConflictException($"Sản phẩm '{product.Name}' không còn bán.");

            if (item.Quantity > product.Stock)
                throw new ConflictException($"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho.");
        }

        // Create order
        var order = Order.Create(request.UserId, request.ShippingAddress, request.Notes);
        order.SetPaymentMethod(request.PaymentMethod);

        foreach (var item in cart.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            var orderItem = OrderItem.Create(order.Id, item.ProductId, product!.Name, item.Quantity, item.UnitPrice);
            order.AddItem(orderItem);

            // Reduce stock
            product.ReduceStock(item.Quantity);
            productRepository.Update(product);
        }

        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var coupon = await couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken)
                ?? throw new NotFoundException("Coupon", request.CouponCode);

            if (coupon.IsExpired())
                throw new ConflictException($"Coupon '{request.CouponCode}' đã hết hạn.");

            if (!coupon.HasRemaining())
                throw new ConflictException($"Coupon '{request.CouponCode}' đã hết lượt sử dụng.");

            if (!coupon.MeetsMinOrderValue(order.TotalAmount))
                throw new ConflictException($"Đơn hàng chưa đạt giá trị tối thiểu để dùng coupon.");

            var alreadyUsed = await couponRepository.HasUsageByUserAsync(coupon.Id, request.UserId, cancellationToken);
            if (alreadyUsed)
                throw new ConflictException($"Bạn đã sử dụng coupon '{request.CouponCode}' trước đó.");

            decimal discountAmount = coupon.CalculateDiscount(order.TotalAmount);
            order.ApplyCoupon(coupon.Code, discountAmount);
            coupon.Use();

            // Lưu thông tin sử dụng coupon
            var couponUsage = CouponUsage.Create(request.UserId, order.Id, coupon.Id);
            await couponUsageRepository.AddAsync(couponUsage, cancellationToken);
            couponRepository.Update(coupon);
        }

        await orderRepository.AddAsync(order, cancellationToken);

        // Clear cart after successful order
        cart.Clear();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event for email + notifications (fire-and-forget style via MediatR)
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is not null)
        {
            var eventItems = order.Items.Select(i =>
                new OrderEventItemDto(i.ProductName, i.Quantity, i.UnitPrice)).ToList();

            await mediator.Publish(new OrderPlacedEvent(
                OrderId: order.Id,
                UserId: user.Id.ToString(),
                UserEmail: user.Email,
                UserName: $"{user.FirstName} {user.LastName}".Trim(),
                TotalPrice: order.TotalAmount,
                Items: eventItems), cancellationToken);
        }

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            OriginalAmount = order.OriginalAmount,
            DiscountAmount = order.DiscountAmount,
            ShippingAddress = order.ShippingAddress,
            CouponCode = order.CouponCode,
            Notes = order.Notes,
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            PaidAt = order.PaidAt,
            VnpayTransactionId = order.VnpayTransactionId,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }
}
