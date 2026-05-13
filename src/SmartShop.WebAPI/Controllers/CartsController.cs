using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Cart;
using SmartShop.Application.Features.Cart.Commands.AddToCart;
using SmartShop.Application.Features.Cart.Commands.ClearCart;
using SmartShop.Application.Features.Cart.Commands.RemoveFromCart;
using SmartShop.Application.Features.Cart.Commands.UpdateCartItem;
using SmartShop.Application.Features.Cart.Queries.GetCart;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartsController(IMediator mediator) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lấy giỏ hàng của user hiện tại</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CartDto>>> GetCart(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCartQuery(CurrentUserId), ct);
        return Ok(ApiResponse<CartDto>.Ok(result));
    }

    /// <summary>Thêm sản phẩm vào giỏ hàng</summary>
    [HttpPost("items")]
    public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart(
        [FromBody] AddToCartRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AddToCartCommand(CurrentUserId, request.ProductId, request.Quantity, request.SizeId), ct);
        return Ok(ApiResponse<CartDto>.Ok(result));
    }

    /// <summary>Cập nhật số lượng sản phẩm trong giỏ</summary>
    [HttpPut("items/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(
        Guid productId, [FromBody] UpdateQuantityRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateCartItemCommand(CurrentUserId, productId, request.Quantity, request.SizeId), ct);
        return Ok(ApiResponse<CartDto>.Ok(result));
    }

    /// <summary>Xoá sản phẩm khỏi giỏ hàng</summary>
    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(
        Guid productId, [FromQuery] Guid? sizeId, CancellationToken ct)
    {
        var result = await mediator.Send(new RemoveFromCartCommand(CurrentUserId, productId, sizeId), ct);
        return Ok(ApiResponse<CartDto>.Ok(result));
    }

    /// <summary>Xoá toàn bộ giỏ hàng</summary>
    [HttpDelete]
    public async Task<ActionResult<ApiResponse<object?>>> ClearCart(CancellationToken ct)
    {
        await mediator.Send(new ClearCartCommand(CurrentUserId), ct);
        return Ok(ApiResponse.Ok("Giỏ hàng đã được xoá."));
    }
}

public record AddToCartRequest(Guid ProductId, int Quantity, Guid? SizeId = null);
public record UpdateQuantityRequest(int Quantity, Guid? SizeId = null);
