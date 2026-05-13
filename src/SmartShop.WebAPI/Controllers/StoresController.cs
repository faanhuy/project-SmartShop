using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Inventory.Commands.SetStoreSizeInventory;
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
public class StoresController(
    IMediator mediator,
    IStoreInventoryRepository inventoryRepository,
    IStoreSizeInventoryRepository sizeInventoryRepository) : ControllerBase
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

    /// <summary>Tồn kho theo từng size của một sản phẩm tại chi nhánh</summary>
    [HttpGet("api/stores/{storeId:guid}/products/{productId:guid}/sizes/stock")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<SizeStockDto>>>> GetSizeStock(
        Guid storeId, Guid productId, CancellationToken ct)
    {
        var inventories = await sizeInventoryRepository.GetByProductIdAsync(productId, ct);
        var result = inventories
            .Where(i => i.StoreId == storeId)
            .Select(i => new SizeStockDto(productId, storeId, i.SizeId, i.Quantity))
            .ToList();

        return Ok(ApiResponse<List<SizeStockDto>>.Ok(result));
    }

    // ── Admin endpoints ───────────────────────────────────────────────────

    /// <summary>Tạo chi nhánh mới (Admin only)</summary>
    [HttpPost("api/admin/stores")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> CreateStore(
        [FromBody] CreateStoreRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateStoreCommand(
            request.Name, request.Address, request.Phone,
            request.ProvinceId, request.WardId, request.Street), ct);
        return Ok(result);
    }

    /// <summary>Cập nhật thông tin chi nhánh (Admin only)</summary>
    [HttpPut("api/admin/stores/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> UpdateStore(
        Guid id, [FromBody] UpdateStoreRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateStoreCommand(
            id, request.Name, request.Address, request.Phone, request.IsActive,
            request.ProvinceId, request.WardId, request.Street), ct);
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

    /// <summary>Set tồn kho theo size tại chi nhánh (Admin only)</summary>
    [HttpPut("api/admin/stores/{storeId:guid}/inventory/{productId:guid}/sizes/{sizeId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> SetSizeInventory(
        Guid storeId, Guid productId, Guid sizeId,
        [FromBody] SetSizeInventoryRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new SetStoreSizeInventoryCommand(storeId, productId, sizeId, request.Quantity), ct);
        return Ok(result);
    }
}

public record StockDto(Guid ProductId, Guid StoreId, int Quantity);
public record SizeStockDto(Guid ProductId, Guid StoreId, Guid SizeId, int Quantity);
public record CreateStoreRequest(
    string Name,
    string Address,
    string Phone,
    int? ProvinceId = null,
    int? WardId = null,
    string? Street = null);

public record UpdateStoreRequest(
    string Name,
    string Address,
    string Phone,
    bool IsActive,
    int? ProvinceId = null,
    int? WardId = null,
    string? Street = null);
public record UpdateInventoryRequest(int Quantity);
public record SetSizeInventoryRequest(int Quantity);
