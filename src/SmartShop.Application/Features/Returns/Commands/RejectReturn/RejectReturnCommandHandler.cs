using MediatR;
using Microsoft.Extensions.Logging;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Returns.Commands.RejectReturn;

public class RejectReturnCommandHandler(
    IReturnRequestRepository returnRequestRepository,
    IOrderRepository orderRepository,
    INotificationRepository notificationRepository,
    INotificationHubService hubService,
    IUnitOfWork unitOfWork,
    ILogger<RejectReturnCommandHandler> logger) : IRequestHandler<RejectReturnCommand, ReturnRequestDto>
{
    public async Task<ReturnRequestDto> Handle(
        RejectReturnCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AdminNote, nameof(request.AdminNote));

        var returnRequest = await returnRequestRepository.GetByIdAsync(request.ReturnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return Request", request.ReturnRequestId);

        if (returnRequest.Status != ReturnStatus.Pending)
            throw new ConflictException("Chỉ có thể từ chối yêu cầu trả hàng đang chờ xử lý.");

        var order = await orderRepository.GetByIdAsync(returnRequest.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), returnRequest.OrderId);

        returnRequest.Reject(request.AdminNote);
        returnRequestRepository.Update(returnRequest);

        var userId = returnRequest.UserId.ToString();
        var orderCode = order.Id.ToString()[..8].ToUpper();
        var title = "Yêu cầu trả hàng bị từ chối";
        var message = $"Đơn hàng #{orderCode}: Yêu cầu trả hàng bị từ chối. Lý do: {request.AdminNote}";

        var notification = Notification.Create(userId, title, message, order.Id);
        await notificationRepository.AddAsync(notification, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await hubService.SendToUserAsync(userId, "ReturnRequestUpdated", new
            {
                NotificationId = notification.Id,
                Title = title,
                Message = message,
                OrderId = order.Id,
                Status = "Rejected"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Push SignalR notification cho return request {Id} thất bại.", returnRequest.Id);
        }

        return ReturnRequestMapper.ToDto(returnRequest, order.Id.ToString());
    }
}
