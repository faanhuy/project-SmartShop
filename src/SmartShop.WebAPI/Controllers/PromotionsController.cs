using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Promotions.Combos;
using SmartShop.Application.Features.Promotions.Combos.Commands.CreateComboPromotion;
using SmartShop.Application.Features.Promotions.Combos.Commands.DeleteComboPromotion;
using SmartShop.Application.Features.Promotions.Combos.Commands.UpdateComboPromotion;
using SmartShop.Application.Features.Promotions.Combos.Queries.GetActiveCombos;
using SmartShop.Application.Features.Promotions.Combos.Queries.GetCombos;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
public class PromotionsController(IMediator mediator) : ControllerBase
{
    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Danh sách combo promotions (admin)</summary>
    [HttpGet("api/admin/promotions/combos")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<GetCombosResult>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCombosQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Chi tiết combo promotion (admin)</summary>
    [HttpGet("api/admin/promotions/combos/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ComboPromotionDto>>> GetById(Guid id, CancellationToken ct)
    {
        // Reuse GetCombos but filtered — in small dataset this is acceptable.
        // For now return 404 if not found via list (single-entity query can be added later).
        var result = await mediator.Send(new GetCombosQuery(1, int.MaxValue), ct);
        var combo = result.Data?.Items.FirstOrDefault(c => c.Id == id);
        if (combo is null)
            return NotFound(ApiResponse<object?>.Fail("Combo không tồn tại."));

        return Ok(ApiResponse<ComboPromotionDto>.Ok(combo));
    }

    /// <summary>Tạo combo promotion mới</summary>
    [HttpPost("api/admin/promotions/combos")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ComboPromotionDto>> Create(
        [FromBody] CreateComboPromotionCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>Cập nhật combo promotion</summary>
    [HttpPut("api/admin/promotions/combos/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ComboPromotionDto>> Update(
        Guid id, [FromBody] UpdateComboPromotionCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id)
            return BadRequest(ApiResponse<object?>.Fail("Id trong URL không khớp với body."));

        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>Xóa combo promotion</summary>
    [HttpDelete("api/admin/promotions/combos/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteComboPromotionCommand(id), ct);
        return Ok(ApiResponse.Ok("Combo đã được xóa."));
    }

    // ── Public endpoints ──────────────────────────────────────────────────

    /// <summary>Danh sách combo đang active tại chi nhánh</summary>
    [HttpGet("api/promotions/combos/active")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ComboPromotionDto>>>> GetActive(
        [FromQuery] Guid storeId, CancellationToken ct)
    {
        if (storeId == Guid.Empty)
            return BadRequest(ApiResponse<object?>.Fail("storeId là bắt buộc."));

        var result = await mediator.Send(new GetActiveCombosQuery(storeId), ct);
        return Ok(result);
    }
}
