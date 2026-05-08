using MediatR;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Events;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Orders.Commands.PlaceOrder;

public class PlaceOrderCommandHandler(
    ICartRepository cartRepository,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IStoreRepository storeRepository,
    IStoreInventoryRepository storeInventoryRepository,
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

        // Validate store tồn tại và đang active
        var store = await storeRepository.GetByIdAsync(request.StoreId, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.StoreId);

        if (!store.IsActive)
            throw new ConflictException("Chi nhánh đã tạm ngừng hoạt động.");

        // Validate product IsActive, load products
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = new Dictionary<Guid, Product>();
        foreach (var item in cart.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new NotFoundException("Product", item.ProductId);

            if (!product.IsActive)
                throw new ConflictException($"Sản phẩm '{product.Name}' không còn bán.");

            products[item.ProductId] = product;
        }

        // Load tất cả StoreInventory trong 1 query
        var inventories = (await storeInventoryRepository.GetByStoreAndProductsAsync(
            request.StoreId, productIds, cancellationToken))
            .ToDictionary(i => i.ProductId);

        // Phase 1 — Validate stock (read-only)
        ValidateStock(cart.Items, products, inventories);

        // Tạo Order entity
        var order = Order.Create(request.UserId, request.ShippingAddress, request.Notes);
        order.SetStoreId(request.StoreId);
        order.SetPaymentMethod(request.PaymentMethod);

        foreach (var item in cart.Items)
        {
            var product = products[item.ProductId];
            var orderItem = OrderItem.Create(order.Id, item.ProductId, product.Name, item.Quantity, item.UnitPrice);
            order.AddItem(orderItem);
        }

        // Phase 2 — Deduct stock (sort by ProductId để tránh deadlock)
        DeductStock(cart.Items, inventories);

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

            var couponUsage = CouponUsage.Create(request.UserId, order.Id, coupon.Id);
            await couponUsageRepository.AddAsync(couponUsage, cancellationToken);
            couponRepository.Update(coupon);
        }

        await orderRepository.AddAsync(order, cancellationToken);

        cart.Clear();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            // Retry once: reload fresh inventories → validate → deduct → save
            var freshInventories = (await storeInventoryRepository.GetByStoreAndProductsAsync(
                request.StoreId, productIds, cancellationToken))
                .ToDictionary(i => i.ProductId);

            ValidateStock(cart.Items, products, freshInventories);
            DeductStock(cart.Items, freshInventories);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                throw new ConflictException("Sản phẩm vừa hết hàng, vui lòng thử lại.");
            }
        }

        // Publish event for email + notifications
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

    private static void ValidateStock(
        IEnumerable<CartItem> items,
        Dictionary<Guid, Product> products,
        Dictionary<Guid, StoreInventory> inventories)
    {
        foreach (var item in items)
        {
            var productName = products.TryGetValue(item.ProductId, out var p) ? p.Name : item.ProductId.ToString();

            if (!inventories.TryGetValue(item.ProductId, out var inventory))
                throw new ConflictException($"Sản phẩm '{productName}' không có trong kho chi nhánh này.");

            if (item.Quantity > inventory.Quantity)
                throw new ConflictException(
                    $"Sản phẩm '{productName}' chỉ còn {inventory.Quantity} trong kho.");
        }
    }

    private static void DeductStock(
        IEnumerable<CartItem> items,
        Dictionary<Guid, StoreInventory> inventories)
    {
        foreach (var item in items.OrderBy(i => i.ProductId))
        {
            inventories[item.ProductId].DeductStock(item.Quantity);
        }
    }
}
