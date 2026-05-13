using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Products;
using SmartShop.Application.Features.Products.Commands.AddProductSize;
using SmartShop.Application.Features.Products.Commands.DeleteProductSize;
using SmartShop.Application.Features.Products.Commands.SetProductSizes;
using SmartShop.Application.Features.Products.Commands.UpdateProductSize;
using SmartShop.Application.Features.Products.Queries.GetProductSizes;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
public class ProductSizesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lấy danh sách sizes của một sản phẩm</summary>
    [HttpGet("api/products/{productId:guid}/sizes")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<ProductSizeDto>>>> GetSizes(
        Guid productId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductSizesQuery(productId), ct);
        return Ok(result);
    }

    /// <summary>Thêm size mới cho sản phẩm (Admin only)</summary>
    [HttpPost("api/admin/products/{productId:guid}/sizes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductSizeDto>>> AddSize(
        Guid productId, [FromBody] AddSizeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddProductSizeCommand(productId, request.SizeLabel, request.DisplayOrder), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật size (Admin only)</summary>
    [HttpPut("api/admin/products/{productId:guid}/sizes/{sizeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductSizeDto>>> UpdateSize(
        Guid productId, Guid sizeId, [FromBody] UpdateSizeRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateProductSizeCommand(sizeId, request.SizeLabel, request.DisplayOrder), ct);
        return Ok(result);
    }

    /// <summary>Gán danh sách kích cỡ từ master cho sản phẩm (thay thế toàn bộ)</summary>
    [HttpPut("api/admin/products/{productId:guid}/sizes")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<ProductSizeDto>>>> SetSizes(
        Guid productId, [FromBody] SetSizesRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new SetProductSizesCommand(productId, request.SizeIds), ct);
        return Ok(result);
    }

    /// <summary>Xóa size (Admin only)</summary>
    [HttpDelete("api/admin/products/{productId:guid}/sizes/{sizeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSize(
        Guid productId, Guid sizeId, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductSizeCommand(sizeId), ct);
        return Ok(result);
    }
}

public record AddSizeRequest(string SizeLabel, int DisplayOrder);
public record UpdateSizeRequest(string SizeLabel, int DisplayOrder);
public record SetSizesRequest(List<Guid> SizeIds);
