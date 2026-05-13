using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory;
using SmartShop.Application.Features.Inventory.Commands.CancelStockReceipt;
using SmartShop.Application.Features.Inventory.Commands.CompleteStockReceipt;
using SmartShop.Application.Features.Inventory.Commands.CreateStockReceipt;
using SmartShop.Application.Features.Inventory.Commands.DeleteStockReceipt;
using SmartShop.Application.Features.Inventory.Commands.UpdateStockReceipt;
using SmartShop.Application.Features.Inventory.Queries.GetStockReceiptById;
using SmartShop.Application.Features.Inventory.Queries.GetStockReceipts;
using SmartShop.Domain.Enums;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
public class StockReceiptsController(IMediator mediator) : ControllerBase
{
    /// <summary>Tạo phiếu nhập hàng mới</summary>
    [HttpPost("api/admin/stock-receipts")]
    public async Task<ActionResult<ApiResponse<StockReceiptDto>>> CreateReceipt(
        [FromBody] CreateStockReceiptCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Danh sách phiếu nhập hàng của một cửa hàng</summary>
    [HttpGet("api/admin/stock-receipts")]
    public async Task<ActionResult<ApiResponse<PagedResult<StockReceiptDto>>>> GetReceipts(
        [FromQuery] Guid storeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ReceiptStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetStockReceiptsQuery(storeId, page, pageSize, status),
            ct
        );
        return Ok(result);
    }

    /// <summary>Chi tiết phiếu nhập hàng</summary>
    [HttpGet("api/admin/stock-receipts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockReceiptDetailDto>>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetStockReceiptByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Hoàn thành phiếu nhập hàng (cộng tồn kho)</summary>
    [HttpPost("api/admin/stock-receipts/{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<StockReceiptDto>>> CompleteReceipt(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CompleteStockReceiptCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Xóa phiếu nhập hàng (chỉ Pending)</summary>
    [HttpDelete("api/admin/stock-receipts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReceipt(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteStockReceiptCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Hủy phiếu nhập hàng (Pending hoặc Completed; nếu Completed sẽ giảm tồn kho)</summary>
    [HttpPost("api/admin/stock-receipts/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<StockReceiptDto>>> CancelReceipt(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CancelStockReceiptCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật phiếu nhập hàng (chỉ Pending)</summary>
    [HttpPut("api/admin/stock-receipts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StockReceiptDetailDto>>> UpdateReceipt(
        Guid id,
        [FromBody] UpdateStockReceiptCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command with { Id = id }, ct);
        return Ok(result);
    }
}
