using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Returns.Commands.CreateReturnRequest;

public class CreateReturnRequestCommandHandler(
    IOrderRepository orderRepository,
    IReturnRequestRepository returnRequestRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateReturnRequestCommand, ReturnRequestDto>
{
    public async Task<ReturnRequestDto> Handle(
        CreateReturnRequestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUserService.UserId);

        // Get order and validate it exists
        var order = await orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Validate order belongs to current user
        if (order.UserId != userId)
            throw new UnauthorizedException("Bạn không có quyền tạo yêu cầu trả hàng cho đơn hàng này.");

        // Validate order is eligible for return:
        // Case 1 — Delivered: allow within 7 days of delivery
        // Case 2 — Paid but not yet shipped: allow immediate return/refund
        var preShipStatuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing };
        bool isDelivered = order.Status == OrderStatus.Delivered;
        bool isPaidBeforeShip = order.PaymentStatus == PaymentStatus.Paid
            && Array.Exists(preShipStatuses, s => s == order.Status);

        if (!isDelivered && !isPaidBeforeShip)
            throw new ConflictException("Chỉ có thể yêu cầu hoàn tiền khi đơn hàng đã giao hoặc đã thanh toán và chưa được vận chuyển.");

        if (isDelivered)
        {
            if (order.DeliveredAt == null)
                throw new ConflictException("Đơn hàng chưa được đánh dấu là đã giao.");

            var daysSinceDelivery = (DateTime.UtcNow - order.DeliveredAt.Value).Days;
            if (daysSinceDelivery > 7)
                throw new ConflictException("Thời hạn trả hàng là 7 ngày kể từ khi giao. Bạn không thể trả hàng nữa.");
        }

        // Check if a return request already exists for this order
        var existingReturn = await returnRequestRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingReturn != null && existingReturn.Status != ReturnStatus.Rejected)
            throw new ConflictException("Đã có yêu cầu trả hàng đang chờ xử lý hoặc đã được duyệt cho đơn hàng này.");

        var refundAmount = order.TotalAmount;
        ReturnRequest returnRequest;

        if (existingReturn != null)
        {
            // Reuse the rejected record to avoid violating the unique index on OrderId
            existingReturn.Resubmit(request.Reason, request.Description, request.EvidenceImageUrl, refundAmount);
            returnRequestRepository.Update(existingReturn);
            returnRequest = existingReturn;
        }
        else
        {
            returnRequest = ReturnRequest.Create(
                request.OrderId,
                userId,
                request.Reason,
                request.Description,
                request.EvidenceImageUrl,
                refundAmount);
            await returnRequestRepository.AddAsync(returnRequest, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ReturnRequestMapper.ToDto(returnRequest, order.Id.ToString());
    }
}
