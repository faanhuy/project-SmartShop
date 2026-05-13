using MediatR;
using SmartShop.Application.Interfaces;
using SmartShop.Application.Services;
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
    IStoreSizeInventoryRepository storeSizeInventoryRepository,
    ICouponRepository couponRepository,
    ICouponUsageRepository couponUsageRepository,
    IUserRepository userRepository,
    IUserAddressRepository userAddressRepository,
    IPriceCampaignRepository priceCampaignRepository,
    IComboPromotionService comboPromotionService,
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
        var products = new Dictionary<Guid, Product>();
        foreach (var item in cart.Items)
        {
            var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken)
                ?? throw new NotFoundException("Product", item.ProductId);

            if (!product.IsActive)
                throw new ConflictException($"Sản phẩm '{product.Name}' không còn bán.");

            products[item.ProductId] = product;
        }

        // Separate items into sized vs non-sized
        var sizedItems = cart.Items.Where(i => i.SizeId.HasValue).ToList();

        // Load StoreInventory as product total stock for both sized and non-sized items
        var inventoryProductIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var inventories = inventoryProductIds.Count > 0
            ? (await storeInventoryRepository.GetByStoreAndProductsAsync(
                request.StoreId, inventoryProductIds, cancellationToken))
                .ToDictionary(i => i.ProductId)
            : new Dictionary<Guid, StoreInventory>();

        // Load StoreSizeInventory for sized items
        var sizeIds = sizedItems.Select(i => i.SizeId!.Value).ToList();
        var sizeInventories = sizedItems.Count > 0
            ? (await storeSizeInventoryRepository.GetByStoreAndSizesAsync(
                request.StoreId, sizeIds, cancellationToken))
                .ToDictionary(i => i.SizeId)
            : new Dictionary<Guid, StoreSizeInventory>();

        // Phase 1 — Validate stock (read-only)
        ValidateStock(cart.Items, products, inventories, sizeInventories);

        // Load address và build shipping info
        var address = await userAddressRepository.GetByIdAsync(request.AddressId, cancellationToken)
            ?? throw new NotFoundException("Address", request.AddressId);

        var wardName = address.WardEntity?.Name ?? address.Ward;
        var provinceName = address.Province?.Name ?? address.City;

        var shippingAddress = string.Join(", ", new[]
        {
            address.RecipientName,
            address.Phone,
            address.Street,
            wardName,
            provinceName
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // Tạo Order entity
        var order = Order.Create(
            request.UserId,
            shippingAddress,
            request.Notes,
            shippingStreet: address.Street,
            shippingWardId: address.WardId,
            shippingProvinceId: address.ProvinceId,
            shippingAddressId: address.Id);
        order.SetStoreId(request.StoreId);
        order.SetPaymentMethod(request.PaymentMethod);

        // Load effective prices for all cart items at this store
        var priceKeys = cart.Items.Select(i => (i.ProductId, i.SizeId)).ToList();
        var effectivePriceRules = await priceCampaignRepository.GetEffectivePriceItemsAsync(
            request.StoreId, priceKeys, DateTime.UtcNow, cancellationToken);

        foreach (var item in cart.Items)
        {
            var product = products[item.ProductId];
            var basePrice = product.Price;
            var key = (item.ProductId, item.SizeId);

            decimal unitPrice = basePrice;
            decimal? originalUnitPrice = null;

            if (effectivePriceRules.TryGetValue(key, out var rule))
            {
                var promoPrice = (PriceRuleType)rule.ruleType switch
                {
                    PriceRuleType.Coefficient => basePrice * rule.discountValue,
                    PriceRuleType.FixedPrice => rule.discountValue,
                    _ => basePrice
                };

                unitPrice = promoPrice;
                originalUnitPrice = basePrice;
            }

            var orderItem = OrderItem.Create(
                order.Id, item.ProductId, product.Name, item.Quantity, unitPrice,
                sizeId: item.SizeId,
                sizeLabel: item.SizeLabel,
                originalUnitPrice: originalUnitPrice ?? basePrice);
            order.AddItem(orderItem);
        }

        // Phase 2 — Deduct stock (sort by key để tránh deadlock)
        DeductStock(cart.Items, inventories, sizeInventories);

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

        // Combo check — only when no coupon applied (no stacking)
        if (request.ApplyCombo && string.IsNullOrEmpty(request.CouponCode))
        {
            var cartInputs = cart.Items.Select(i => new CartItemInput(i.ProductId, i.SizeId, i.Quantity));
            var comboMatch = await comboPromotionService.FindApplicableComboAsync(
                request.StoreId, cartInputs, cancellationToken);

            if (comboMatch is not null)
            {
                if (comboMatch.RewardType == ComboRewardType.FreeProduct &&
                    comboMatch.FreeProductId.HasValue && comboMatch.FreeQuantity > 0)
                {
                    var rewardProduct = await productRepository.GetByIdAsync(comboMatch.FreeProductId.Value, cancellationToken)
                        ?? throw new NotFoundException(nameof(Product), comboMatch.FreeProductId.Value);

                    // Deduct reward product inventory
                    if (comboMatch.FreeSizeId.HasValue)
                    {
                        if (!sizeInventories.TryGetValue(comboMatch.FreeSizeId.Value, out var rewardSizeInv))
                        {
                            // Load fresh if not already loaded
                            var freshSizeInvList = await storeSizeInventoryRepository
                                .GetByStoreAndSizesAsync(request.StoreId, [comboMatch.FreeSizeId.Value], cancellationToken);
                            rewardSizeInv = freshSizeInvList.FirstOrDefault()
                                ?? throw new NotFoundException("Reward product out of stock", comboMatch.FreeSizeId.Value);
                            sizeInventories[comboMatch.FreeSizeId.Value] = rewardSizeInv;
                        }

                        if (rewardSizeInv.Quantity < comboMatch.FreeQuantity)
                            throw new ConflictException("Sản phẩm tặng kèm đã hết hàng.");

                        rewardSizeInv.DeductStock(comboMatch.FreeQuantity);

                        if (!inventories.TryGetValue(comboMatch.FreeProductId.Value, out var rewardTotalInv))
                        {
                            var freshInvList = await storeInventoryRepository
                                .GetByStoreAndProductsAsync(request.StoreId, [comboMatch.FreeProductId.Value], cancellationToken);
                            rewardTotalInv = freshInvList.FirstOrDefault()
                                ?? throw new ConflictException("Sản phẩm tặng kèm chưa có tồn tổng tại chi nhánh này.");
                            inventories[comboMatch.FreeProductId.Value] = rewardTotalInv;
                        }

                        if (rewardTotalInv.Quantity < comboMatch.FreeQuantity)
                            throw new ConflictException("Sản phẩm tặng kèm đã hết hàng.");

                        rewardTotalInv.DeductStock(comboMatch.FreeQuantity);
                    }
                    else
                    {
                        if (!inventories.TryGetValue(comboMatch.FreeProductId.Value, out var rewardInv))
                        {
                            var freshInvList = await storeInventoryRepository
                                .GetByStoreAndProductsAsync(request.StoreId, [comboMatch.FreeProductId.Value], cancellationToken);
                            rewardInv = freshInvList.FirstOrDefault()
                                ?? throw new NotFoundException("Reward product out of stock", comboMatch.FreeProductId.Value);
                            inventories[comboMatch.FreeProductId.Value] = rewardInv;
                        }

                        if (rewardInv.Quantity < comboMatch.FreeQuantity)
                            throw new ConflictException("Sản phẩm tặng kèm đã hết hàng.");

                        rewardInv.DeductStock(comboMatch.FreeQuantity);
                    }

                    // Add free OrderItem with price = 0
                    var freeItem = OrderItem.Create(
                        order.Id,
                        rewardProduct.Id,
                        rewardProduct.Name,
                        comboMatch.FreeQuantity,
                        unitPrice: 0m,
                        sizeId: comboMatch.FreeSizeId,
                        sizeLabel: null,
                        originalUnitPrice: rewardProduct.Price);
                    order.AddItem(freeItem);
                }
                else if (comboMatch.RewardType == ComboRewardType.DiscountAmount && comboMatch.DiscountAmount > 0)
                {
                    order.ApplyComboDiscount(comboMatch.Combo.Id, comboMatch.DiscountAmount);
                }
            }
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
            var freshInventories = inventoryProductIds.Count > 0
                ? (await storeInventoryRepository.GetByStoreAndProductsAsync(
                    request.StoreId, inventoryProductIds, cancellationToken))
                    .ToDictionary(i => i.ProductId)
                : new Dictionary<Guid, StoreInventory>();

            var freshSizeInventories = sizedItems.Count > 0
                ? (await storeSizeInventoryRepository.GetByStoreAndSizesAsync(
                    request.StoreId, sizeIds, cancellationToken))
                    .ToDictionary(i => i.SizeId)
                : new Dictionary<Guid, StoreSizeInventory>();

            ValidateStock(cart.Items, products, freshInventories, freshSizeInventories);
            DeductStock(cart.Items, freshInventories, freshSizeInventories);

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
            ShippingAddressId = order.ShippingAddressId,
            ShippingStreet = order.ShippingStreet,
            ShippingWardId = order.ShippingWardId,
            ShippingProvinceId = order.ShippingProvinceId,
            ShippingWardName = wardName,
            ShippingProvinceName = provinceName,
            CouponCode = order.CouponCode,
            ComboPromotionId = order.ComboPromotionId,
            ComboDiscountAmount = order.ComboDiscountAmount,
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
                SubTotal = i.SubTotal,
                SizeId = i.SizeId,
                SizeLabel = i.SizeLabel,
                OriginalUnitPrice = i.OriginalUnitPrice
            }).ToList(),
            CreatedAt = order.CreatedAt
        };
    }

    private static void ValidateStock(
        IEnumerable<CartItem> items,
        Dictionary<Guid, Product> products,
        Dictionary<Guid, StoreInventory> inventories,
        Dictionary<Guid, StoreSizeInventory> sizeInventories)
    {
        foreach (var item in items)
        {
            var productName = products.TryGetValue(item.ProductId, out var p) ? p.Name : item.ProductId.ToString();

            if (item.SizeId.HasValue)
            {
                if (!sizeInventories.TryGetValue(item.SizeId.Value, out var sizeInv))
                    throw new ConflictException(
                        $"Sản phẩm '{productName}' size {item.SizeLabel} không có trong kho chi nhánh này.");

                if (item.Quantity > sizeInv.Quantity)
                    throw new ConflictException(
                        $"Sản phẩm '{productName}' size {item.SizeLabel} chỉ còn {sizeInv.Quantity} trong kho.");
            }
            else
            {
                if (!inventories.TryGetValue(item.ProductId, out var inventory))
                    throw new ConflictException($"Sản phẩm '{productName}' không có trong kho chi nhánh này.");

                if (item.Quantity > inventory.Quantity)
                    throw new ConflictException(
                        $"Sản phẩm '{productName}' chỉ còn {inventory.Quantity} trong kho.");
            }
        }
    }

    private static void DeductStock(
        IEnumerable<CartItem> items,
        Dictionary<Guid, StoreInventory> inventories,
        Dictionary<Guid, StoreSizeInventory> sizeInventories)
    {
        foreach (var item in items.OrderBy(i => i.SizeId.HasValue ? i.SizeId.ToString() : i.ProductId.ToString()))
        {
            if (item.SizeId.HasValue)
            {
                sizeInventories[item.SizeId.Value].DeductStock(item.Quantity);
                if (!inventories.TryGetValue(item.ProductId, out var inventory))
                    throw new ConflictException("Sản phẩm chưa có tồn tổng tại chi nhánh này.");

                inventory.DeductStock(item.Quantity);
            }
            else
            {
                inventories[item.ProductId].DeductStock(item.Quantity);
            }
        }
    }
}
