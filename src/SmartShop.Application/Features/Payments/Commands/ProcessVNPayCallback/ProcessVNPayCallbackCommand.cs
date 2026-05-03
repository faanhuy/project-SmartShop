using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Payments.Commands.ProcessVNPayCallback;

public record ProcessVNPayCallbackCommand(IDictionary<string, string> QueryParams) : IRequest<ApiResponse<bool>>;
