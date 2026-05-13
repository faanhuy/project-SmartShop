using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory;
using SmartShop.Application.Features.Inventory.Commands.CreateSize;
using SmartShop.Application.Features.Inventory.Commands.DeleteSize;
using SmartShop.Application.Features.Inventory.Commands.UpdateSize;
using SmartShop.Application.Features.Inventory.Queries.GetSizes;
using SmartShop.Domain.Enums;
using SmartShop.WebAPI.Controllers.Requests;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
public class SizesController(IMediator mediator) : ControllerBase
{
    // ── Public endpoints ──────────────────────────────────────────────────

    /// <summary>Danh sách kích thước đang hoạt động</summary>
    [HttpGet("api/sizes")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<SizeDto>>>> GetSizes(
        [FromQuery] SizeType? category = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetSizesQuery(category), ct);
        return Ok(result);
    }

    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Danh sách tất cả kích thước (kể cả inactive)</summary>
    [HttpGet("api/admin/sizes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<SizeDto>>>> GetAllSizes(CancellationToken ct)
    {
        // Query all by requesting with null category - will return all sizes in handler
        var result = await mediator.Send(new GetSizesQuery(null), ct);
        return Ok(result);
    }

    /// <summary>Tạo kích thước mới</summary>
    [HttpPost("api/admin/sizes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SizeDto>>> CreateSize(
        [FromBody] CreateSizeCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Cập nhật kích thước</summary>
    [HttpPut("api/admin/sizes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SizeDto>>> UpdateSize(
        Guid id,
        [FromBody] UpdateSizeMasterRequest request,
        CancellationToken ct)
    {
        var command = new UpdateSizeCommand(id, request.Label, request.DisplayOrder);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Vô hiệu hóa kích thước</summary>
    [HttpDelete("api/admin/sizes/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteSize(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteSizeCommand(id), ct);
        return Ok(result);
    }
}
