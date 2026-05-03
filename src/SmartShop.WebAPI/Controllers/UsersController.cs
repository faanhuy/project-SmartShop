using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Addresses;
using SmartShop.Application.Features.Addresses.Commands.AddAddress;
using SmartShop.Application.Features.Addresses.Commands.DeleteAddress;
using SmartShop.Application.Features.Addresses.Commands.SetDefaultAddress;
using SmartShop.Application.Features.Addresses.Commands.UpdateAddress;
using SmartShop.Application.Features.Addresses.Queries.GetAddresses;
using SmartShop.Application.Features.Users;
using SmartShop.Application.Features.Users.Commands.UpdateMyProfile;
using SmartShop.Application.Features.Users.Queries.GetMyProfile;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IMediator mediator) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Profile ───────────────────────────────────────────────────────────

    /// <summary>Lấy thông tin profile của user hiện tại</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMyProfile(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyProfileQuery(Guid.Parse(CurrentUserId)), ct);
        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    /// <summary>Cập nhật họ tên của user hiện tại</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new UpdateMyProfileCommand(Guid.Parse(CurrentUserId), request.FirstName, request.LastName), ct);
        return Ok(ApiResponse<UserProfileDto>.Ok(result));
    }

    // ── Addresses ─────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách địa chỉ giao hàng của user</summary>
    [HttpGet("me/addresses")]
    public async Task<ActionResult<ApiResponse<List<AddressDto>>>> GetAddresses(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAddressesQuery(CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Thêm địa chỉ giao hàng mới</summary>
    [HttpPost("me/addresses")]
    public async Task<ActionResult<ApiResponse<AddressDto>>> AddAddress(
        [FromBody] AddAddressRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new AddAddressCommand(
            CurrentUserId,
            request.Label,
            request.RecipientName,
            request.Phone,
            request.Street,
            request.Ward,
            request.District,
            request.City), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật địa chỉ giao hàng</summary>
    [HttpPut("me/addresses/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AddressDto>>> UpdateAddress(
        Guid id, [FromBody] UpdateAddressRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateAddressCommand(
            id,
            CurrentUserId,
            request.Label,
            request.RecipientName,
            request.Phone,
            request.Street,
            request.Ward,
            request.District,
            request.City), ct);
        return Ok(result);
    }

    /// <summary>Xóa địa chỉ giao hàng</summary>
    [HttpDelete("me/addresses/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteAddress(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteAddressCommand(id, CurrentUserId), ct);
        return Ok(result);
    }

    /// <summary>Đặt địa chỉ làm mặc định</summary>
    [HttpPatch("me/addresses/{id:guid}/default")]
    public async Task<ActionResult<ApiResponse<bool>>> SetDefaultAddress(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new SetDefaultAddressCommand(id, CurrentUserId), ct);
        return Ok(result);
    }
}

public record UpdateProfileRequest(string FirstName, string LastName);
