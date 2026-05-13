using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.PriceCampaigns;
using SmartShop.Application.Features.PriceCampaigns.Commands.CreatePriceCampaign;
using SmartShop.Application.Features.PriceCampaigns.Commands.DeletePriceCampaign;
using SmartShop.Application.Features.PriceCampaigns.Commands.UpdatePriceCampaign;
using SmartShop.Application.Features.PriceCampaigns.Queries.GetBulkEffectivePrices;
using SmartShop.Application.Features.PriceCampaigns.Queries.GetPriceCampaignById;
using SmartShop.Application.Features.PriceCampaigns.Queries.GetPriceLists;
using SmartShop.Application.Products.Queries.GetProducts;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
public class PriceCampaignsController(IMediator mediator) : ControllerBase
{
    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Danh sách bảng giá (admin)</summary>
    [HttpGet("api/admin/price-campaigns")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<PriceCampaignSummaryDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPriceListsQuery(page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Chi tiết bảng giá (admin)</summary>
    [HttpGet("api/admin/price-campaigns/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PriceCampaignDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPriceCampaignByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Tạo bảng giá mới</summary>
    [HttpPost("api/admin/price-campaigns")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PriceCampaignDto>>> Create(
        [FromBody] CreatePriceCampaignCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>Cập nhật bảng giá</summary>
    [HttpPut("api/admin/price-campaigns/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PriceCampaignDto>>> Update(
        Guid id, [FromBody] UpdatePriceCampaignCommand cmd, CancellationToken ct)
    {
        if (id != cmd.Id)
            return BadRequest(ApiResponse<object?>.Fail("Id trong URL không khớp với body."));

        var result = await mediator.Send(cmd, ct);
        return Ok(result);
    }

    /// <summary>Xóa bảng giá</summary>
    [HttpDelete("api/admin/price-campaigns/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeletePriceCampaignCommand(id), ct);
        return Ok(result);
    }

    // ── Public endpoints ──────────────────────────────────────────────────

    /// <summary>Lấy giá hiệu quả cho nhiều sản phẩm tại 1 chi nhánh</summary>
    [HttpPost("api/price-campaigns/bulk-effective-prices")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<BulkEffectivePriceResult>>>> GetBulkEffectivePrices(
        [FromBody] GetBulkEffectivePricesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }
}
