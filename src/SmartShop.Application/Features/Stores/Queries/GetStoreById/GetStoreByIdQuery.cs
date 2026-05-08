using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;

namespace SmartShop.Application.Features.Stores.Queries.GetStoreById;

public record GetStoreByIdQuery(Guid Id) : IRequest<ApiResponse<StoreDto>>;
