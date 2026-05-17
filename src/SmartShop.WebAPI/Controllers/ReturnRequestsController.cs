using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Returns;
using SmartShop.Application.Features.Returns.Commands.ApproveReturn;
using SmartShop.Application.Features.Returns.Commands.CreateReturnRequest;
using SmartShop.Application.Features.Returns.Commands.RejectReturn;
using SmartShop.Application.Features.Returns.Queries.GetAllReturnRequests;
using SmartShop.Application.Features.Returns.Queries.GetMyReturnRequests;
using SmartShop.Domain.Enums;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ReturnRequestsController(IMediator mediator) : ControllerBase
{
    /// <summary>Tạo yêu cầu trả hàng cho đơn hàng đã giao</summary>
    [HttpPost("orders/{orderId:guid}/return-request")]
    public async Task<ActionResult<ApiResponse<ReturnRequestDto>>> CreateReturnRequest(
        Guid orderId,
        [FromBody] CreateReturnRequestRequest request,
        CancellationToken ct)
    {
        var command = new CreateReturnRequestCommand(
            orderId,
            (ReturnReason)request.Reason,
            request.Description,
            request.EvidenceImageUrl);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMyReturnRequests), ApiResponse<ReturnRequestDto>.Ok(result));
    }

    /// <summary>Lấy danh sách yêu cầu trả hàng của người dùng hiện tại</summary>
    [HttpGet("orders/return-requests")]
    public async Task<ActionResult<ApiResponse<List<ReturnRequestDto>>>> GetMyReturnRequests(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyReturnRequestsQuery(), ct);
        return Ok(ApiResponse<List<ReturnRequestDto>>.Ok(result));
    }

    /// <summary>Lấy danh sách tất cả yêu cầu trả hàng (Admin)</summary>
    [HttpGet("admin/return-requests")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<ReturnRequestDto>>>> GetAllReturnRequests(
        [FromQuery] int? status,
        CancellationToken ct)
    {
        ReturnStatus? statusFilter = null;
        if (status.HasValue && Enum.IsDefined(typeof(ReturnStatus), status.Value))
        {
            statusFilter = (ReturnStatus)status.Value;
        }

        var result = await mediator.Send(new GetAllReturnRequestsQuery(statusFilter), ct);
        return Ok(ApiResponse<List<ReturnRequestDto>>.Ok(result));
    }

    /// <summary>Phê duyệt yêu cầu trả hàng (Admin)</summary>
    [HttpPost("admin/return-requests/{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ReturnRequestDto>>> ApproveReturn(
        Guid id,
        [FromBody] ApproveReturnRequest? request,
        CancellationToken ct)
    {
        var command = new ApproveReturnCommand(id, request?.AdminNote);
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<ReturnRequestDto>.Ok(result));
    }

    /// <summary>Từ chối yêu cầu trả hàng (Admin)</summary>
    [HttpPost("admin/return-requests/{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ReturnRequestDto>>> RejectReturn(
        Guid id,
        [FromBody] RejectReturnRequest request,
        CancellationToken ct)
    {
        var command = new RejectReturnCommand(id, request.AdminNote);
        var result = await mediator.Send(command, ct);
        return Ok(ApiResponse<ReturnRequestDto>.Ok(result));
    }
}

public record CreateReturnRequestRequest(
    int Reason,
    string? Description,
    string? EvidenceImageUrl);

public record ApproveReturnRequest(string? AdminNote);

public record RejectReturnRequest(string AdminNote);
