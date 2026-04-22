using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Admin.Analytics.DTOs;
using SmartShop.Application.Features.Admin.Analytics.Queries.GetOrderStatusBreakdown;
using SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueByDate;
using SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueSummary;
using SmartShop.Application.Features.Admin.Analytics.Queries.GetTopProducts;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "Admin")]
public class AnalyticsController(IMediator mediator) : ControllerBase
{
    private static (DateTime From, DateTime To) DefaultRange()
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-30);
        return (from, to);
    }

    /// <summary>Tổng quan doanh thu theo khoảng thời gian</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<RevenueSummaryDto>>> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var (defaultFrom, defaultTo) = DefaultRange();
        var result = await mediator.Send(
            new GetRevenueSummaryQuery(from ?? defaultFrom, to ?? defaultTo), ct);
        return Ok(ApiResponse<RevenueSummaryDto>.Ok(result));
    }

    /// <summary>Doanh thu theo ngày trong khoảng thời gian</summary>
    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueByDateDto>>>> GetRevenueByDate(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var (defaultFrom, defaultTo) = DefaultRange();
        var result = await mediator.Send(
            new GetRevenueByDateQuery(from ?? defaultFrom, to ?? defaultTo), ct);
        return Ok(ApiResponse<IReadOnlyList<RevenueByDateDto>>.Ok(result));
    }

    /// <summary>Top sản phẩm bán chạy theo doanh thu</summary>
    [HttpGet("top-products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopProductDto>>>> GetTopProducts(
        [FromQuery] int limit = 5,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var (defaultFrom, defaultTo) = DefaultRange();
        var result = await mediator.Send(
            new GetTopProductsQuery(from ?? defaultFrom, to ?? defaultTo, limit), ct);
        return Ok(ApiResponse<IReadOnlyList<TopProductDto>>.Ok(result));
    }

    /// <summary>Phân bổ đơn hàng theo trạng thái</summary>
    [HttpGet("order-status")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderStatusBreakdownDto>>>> GetOrderStatusBreakdown(
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetOrderStatusBreakdownQuery(), ct);
        return Ok(ApiResponse<IReadOnlyList<OrderStatusBreakdownDto>>.Ok(result));
    }
}
