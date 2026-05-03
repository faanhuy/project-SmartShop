using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Enums;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Payments.Commands.CreateVNPayPayment;

public class CreateVNPayPaymentCommandHandler(
    IOrderRepository orderRepository,
    IPaymentGateway paymentGateway) : IRequestHandler<CreateVNPayPaymentCommand, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(CreateVNPayPaymentCommand command, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Order), command.OrderId);

        if (order.UserId.ToString() != command.UserId)
            throw new UnauthorizedException("Không có quyền truy cập đơn hàng này.");

        if (order.PaymentMethod != PaymentMethod.VNPay)
            throw new ConflictException("Đơn hàng này không sử dụng phương thức thanh toán VNPay.");

        if (order.PaymentStatus != PaymentStatus.Pending)
            throw new ConflictException("Đơn hàng này đã được xử lý thanh toán.");

        var paymentRequest = new CreatePaymentRequest(
            OrderId: order.Id.ToString(),
            Amount: (long)order.TotalAmount,
            OrderDescription: $"Thanh toan don hang {order.Id}",
            ReturnUrl: command.ReturnUrl,
            IpAddress: command.IpAddress);

        var paymentUrl = paymentGateway.CreatePaymentUrl(paymentRequest);

        return ApiResponse<string>.Ok(paymentUrl, "Tạo link thanh toán thành công.");
    }
}
