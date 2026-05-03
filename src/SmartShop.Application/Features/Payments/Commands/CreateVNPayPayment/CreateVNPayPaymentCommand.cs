using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Payments.Commands.CreateVNPayPayment;

public record CreateVNPayPaymentCommand(
    Guid OrderId,
    string UserId,
    string ReturnUrl,
    string IpAddress) : IRequest<ApiResponse<string>>;
