using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;

namespace SmartShop.Application.Features.Stores.Commands.UpdateStore;

public record UpdateStoreCommand(
    Guid Id,
    string Name,
    string Address,
    string Phone,
    bool IsActive,
    int? ProvinceId = null,
    int? WardId = null,
    string? Street = null) : IRequest<ApiResponse<StoreDto>>;
