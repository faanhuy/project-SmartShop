using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Payments.Commands.ProcessVNPayCallback;

public class ProcessVNPayCallbackCommandHandler(
    IOrderRepository orderRepository,
    IPaymentGateway paymentGateway,
    IUnitOfWork unitOfWork) : IRequestHandler<ProcessVNPayCallbackCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(ProcessVNPayCallbackCommand command, CancellationToken ct)
    {
        var callbackResult = paymentGateway.ProcessCallback(command.QueryParams);

        if (!Guid.TryParse(callbackResult.OrderId, out var orderId))
            throw new NotFoundException(nameof(Domain.Entities.Order), callbackResult.OrderId);

        var order = await orderRepository.GetByIdAsync(orderId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Order), orderId);

        // Idempotency: nếu đã xử lý rồi thì bỏ qua
        if (order.PaymentStatus != PaymentStatus.Pending)
            return ApiResponse<bool>.Ok(order.PaymentStatus == PaymentStatus.Paid);

        if (callbackResult.IsSuccess)
        {
            var vnTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            order.MarkAsPaid(callbackResult.TransactionId,
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTz));
        }
        else
            order.MarkPaymentFailed();

        await unitOfWork.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(callbackResult.IsSuccess);
    }
}
