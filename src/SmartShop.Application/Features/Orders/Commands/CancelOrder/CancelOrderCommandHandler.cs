using MediatR;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    ICouponRepository couponRepository,
    ICouponUsageRepository couponUsageRepository,
    IStoreInventoryRepository storeInventoryRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CancelOrderCommand>
{
    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.UserId != request.UserId)
            throw new UnauthorizedException("Bạn không có quyền huỷ đơn hàng này.");

        if (order.Status != OrderStatus.Pending)
            throw new ConflictException("Chỉ có thể huỷ đơn hàng đang ở trạng thái Chờ xác nhận.");

        order.Cancel();

        // Hoàn lại UsedQuantity nếu đơn hàng có dùng coupon
        if (!string.IsNullOrEmpty(order.CouponCode))
        {
            var coupon = await couponRepository.GetByCodeAsync(order.CouponCode, cancellationToken);
            if (coupon is not null)
            {
                coupon.Refund();
                couponRepository.Update(coupon);
            }

            var usage = await couponUsageRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (usage is not null)
                couponUsageRepository.Delete(usage);
        }

        // Restore StoreInventory nếu order có StoreId
        if (order.StoreId.HasValue)
        {
            foreach (var item in order.Items)
            {
                var inventory = await storeInventoryRepository.GetByStoreAndProductAsync(
                    order.StoreId.Value, item.ProductId, cancellationToken);

                if (inventory is not null)
                    inventory.RestoreStock(item.Quantity);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
