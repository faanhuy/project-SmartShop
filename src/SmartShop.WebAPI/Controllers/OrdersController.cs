using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Orders;
using SmartShop.Application.Features.Orders.Commands.CancelOrder;
using SmartShop.Application.Features.Orders.Commands.PlaceOrder;
using SmartShop.Application.Features.Orders.Commands.UpdateOrderStatus;
using SmartShop.Application.Features.Orders.Queries.GetAllOrders;
using SmartShop.Application.Features.Orders.Queries.GetMyOrders;
using SmartShop.Application.Features.Orders.Queries.GetOrderById;
using SmartShop.Application.Products.Queries.GetProducts;
using SmartShop.Domain.Enums;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IMediator mediator) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Đặt hàng từ giỏ hàng hiện tại</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> PlaceOrder(
        [FromBody] PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new PlaceOrderCommand(CurrentUserId, request.StoreId, request.AddressId,
                request.Notes, request.CouponCode, request.PaymentMethod), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<OrderDto>.Ok(result));
    }

    /// <summary>Lấy danh sách đơn hàng của user hiện tại</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyOrdersQuery(CurrentUserId, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<OrderDto>>.Ok(result));
    }

    /// <summary>Lấy chi tiết một đơn hàng</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderByIdQuery(CurrentUserId, id), ct);
        return Ok(ApiResponse<OrderDto>.Ok(result));
    }

    /// <summary>Huỷ đơn hàng (chỉ khi Pending, chủ đơn)</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<object?>>> Cancel(Guid id, CancellationToken ct)
    {
        await mediator.Send(new CancelOrderCommand(id, CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Đơn hàng đã được huỷ."));
    }

    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Lấy tất cả đơn hàng (Admin only)</summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetAllOrdersQuery(page, pageSize, status), ct);
        return Ok(ApiResponse<PagedResult<OrderDto>>.Ok(result));
    }

    /// <summary>Cập nhật trạng thái đơn hàng (Admin only)</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateOrderStatusCommand(id, request.Status), ct);
        return Ok(ApiResponse<OrderDto>.Ok(result));
    }
}

public record PlaceOrderRequest(
    Guid StoreId,
    Guid AddressId,
    string? Notes,
    string? CouponCode,
    PaymentMethod PaymentMethod = PaymentMethod.COD);
public record UpdateOrderStatusRequest(OrderStatus Status);
