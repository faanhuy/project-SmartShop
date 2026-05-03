using MediatR;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Events;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Orders.EventHandlers;

public class PushSignalRNotificationHandler(
    INotificationHubService hubService,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<PushSignalRNotificationHandler> logger) : INotificationHandler<OrderStatusChangedEvent>
{
    public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var statusVi = notification.NewStatus switch
            {
                "Pending"   => "Chờ xác nhận",
                "Confirmed" => "Đã xác nhận",
                "Shipped"   => "Đang giao hàng",
                "Delivered" => "Đã giao hàng",
                "Cancelled" => "Đã hủy",
                "Processing" => "Đang xử lý",
                "Refunded" => "Đã hoàn tiền",
                _           => notification.NewStatus
            };
            var orderCode = notification.OrderId.ToString()[..8].ToUpper();
            var title = "Cập nhật đơn hàng";
            var message = $"Đơn hàng #{orderCode} đã được cập nhật sang trạng thái: {statusVi}.";

            var dbNotification = Notification.Create(
                userId: notification.UserId,
                title: title,
                message: message,
                orderId: notification.OrderId);

            await notificationRepository.AddAsync(dbNotification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var payload = new
            {
                NotificationId = dbNotification.Id,
                Title = title,
                Message = message,
                OrderId = notification.OrderId
            };

            await hubService.SendToUserAsync(notification.UserId, "OrderStatusUpdated", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Push SignalR notification cho đơn hàng {OrderId} thất bại.",
                notification.OrderId);
        }
    }
}
