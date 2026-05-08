using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory.Commands.UpdateStoreInventory;
using SmartShop.Application.Features.Inventory.Queries.GetLowStockProducts;
using SmartShop.Application.Features.Inventory.Queries.GetStoreInventory;
using SmartShop.Application.Features.Stores.Commands.CreateStore;
using SmartShop.Application.Features.Stores.Commands.UpdateStore;
using SmartShop.Application.Features.Stores.Queries.GetStoreById;
using SmartShop.Application.Features.Stores.Queries.GetStores;
using SmartShop.Domain.Interfaces;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
public class StoresController(IMediator mediator, IStoreInventoryRepository inventoryRepository) : ControllerBase
{
    // ── Public endpoints ──────────────────────────────────────────────────

    /// <summary>Danh sách chi nhánh đang hoạt động</summary>
    [HttpGet("api/stores")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<StoreDto>>>> GetStores(CancellationToken ct)
    {
        var result = await mediator.Send(new GetStoresQuery(), ct);
        return Ok(result);
    }

    /// <summary>Chi tiết một chi nhánh</summary>
    [HttpGet("api/stores/{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<StoreDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetStoreByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Tồn kho của một sản phẩm tại chi nhánh</summary>
    [HttpGet("api/stores/{storeId:guid}/products/{productId:guid}/stock")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<StockDto>>> GetStock(Guid storeId, Guid productId, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByStoreAndProductAsync(storeId, productId, ct);
        var quantity = inventory?.Quantity ?? 0;
        return Ok(ApiResponse<StockDto>.Ok(new StockDto(productId, storeId, quantity)));
    }

    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Tạo chi nhánh mới (Admin only)</summary>
    [HttpPost("api/admin/stores")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> CreateStore(
        [FromBody] CreateStoreRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateStoreCommand(request.Name, request.Address, request.Phone), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật thông tin chi nhánh (Admin only)</summary>
    [HttpPut("api/admin/stores/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> UpdateStore(
        Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateStoreCommand(id, request.Name, request.Address, request.Phone, request.IsActive), ct);
        return Ok(result);
    }

    /// <summary>Toàn bộ tồn kho của một chi nhánh (Admin only)</summary>
    [HttpGet("api/admin/stores/{storeId:guid}/inventory")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<StoreInventoryDto>>>> GetInventory(Guid storeId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetStoreInventoryQuery(storeId), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật tồn kho thủ công (Admin only)</summary>
    [HttpPatch("api/admin/stores/{storeId:guid}/inventory/{productId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StoreInventoryDto>>> UpdateInventory(
        Guid storeId, Guid productId, [FromBody] UpdateInventoryRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateStoreInventoryCommand(storeId, productId, request.Quantity), ct);
        return Ok(result);
    }

    /// <summary>Sản phẩm sắp hết hàng tại chi nhánh (Admin only)</summary>
    [HttpGet("api/admin/stores/{storeId:guid}/inventory/low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<LowStockProductDto>>>> GetLowStock(Guid storeId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLowStockProductsQuery(storeId), ct);
        return Ok(result);
    }
}

public record StockDto(Guid ProductId, Guid StoreId, int Quantity);
public record CreateStoreRequest(string Name, string Address, string Phone);
public record UpdateStoreRequest(string Name, string Address, string Phone, bool IsActive);
public record UpdateInventoryRequest(int Quantity);
