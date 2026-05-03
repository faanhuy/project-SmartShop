using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Payments.Commands.CreateVNPayPayment;
using SmartShop.Application.Features.Payments.Commands.ProcessVNPayCallback;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private string GetClientIpAddress()
    {
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    /// <summary>Tạo URL thanh toán VNPay cho đơn hàng</summary>
    [HttpPost("vnpay/create")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> CreateVNPayPayment(
        [FromBody] CreateVNPayPaymentRequest request, CancellationToken ct)
    {
        var returnUrl = configuration["VNPay:ReturnUrl"]
            ?? $"{Request.Scheme}://{Request.Host}/payment/result";

        var result = await mediator.Send(new CreateVNPayPaymentCommand(
            OrderId: request.OrderId,
            UserId: CurrentUserId,
            ReturnUrl: returnUrl,
            IpAddress: GetClientIpAddress()), ct);

        return Ok(result);
    }

    /// <summary>VNPay callback sau khi thanh toán — redirect về frontend</summary>
    [HttpGet("vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> VNPayCallback(CancellationToken ct)
    {
        // Convert IQueryCollection → IDictionary to avoid ASP.NET Core dependency in Application/Domain
        var queryParams = HttpContext.Request.Query
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        var result = await mediator.Send(new ProcessVNPayCallbackCommand(queryParams), ct);

        var frontendReturnUrl = configuration["VNPay:FrontendResultUrl"]
            ?? "http://localhost:5173/payment/result";

        var separator = frontendReturnUrl.Contains('?') ? '&' : '?';
        var orderId = HttpContext.Request.Query["vnp_TxnRef"].ToString();
        var redirectUrl = $"{frontendReturnUrl}{separator}success={result.Data}&orderId={orderId}";

        return Redirect(redirectUrl);
    }
}

public record CreateVNPayPaymentRequest(Guid OrderId);
