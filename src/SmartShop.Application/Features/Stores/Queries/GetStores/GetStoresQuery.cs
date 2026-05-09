using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Stores.Queries.GetStores;

public record GetStoresQuery : IRequest<ApiResponse<List<StoreDto>>>;

public record StoreDto(
    Guid Id,
    string Name,
    string Address,
    string Phone,
    string? Street = null,
    int? ProvinceId = null,
    int? WardId = null,
    string? ProvinceName = null,
    string? WardName = null);
