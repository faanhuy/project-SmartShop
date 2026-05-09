using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;

namespace SmartShop.Application.Features.Stores.Commands.CreateStore;

public record CreateStoreCommand(
    string Name,
    string Address,
    string Phone,
    int? ProvinceId = null,
    int? WardId = null,
    string? Street = null) : IRequest<ApiResponse<StoreDto>>;
