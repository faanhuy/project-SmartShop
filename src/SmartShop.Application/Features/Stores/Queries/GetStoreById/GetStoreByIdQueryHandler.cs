using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Stores.Queries.GetStoreById;

public class GetStoreByIdQueryHandler(IStoreRepository storeRepository)
    : IRequestHandler<GetStoreByIdQuery, ApiResponse<StoreDto>>
{
    public async Task<ApiResponse<StoreDto>> Handle(GetStoreByIdQuery request, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Store", request.Id);

        return ApiResponse<StoreDto>.Ok(new StoreDto(
            store.Id, store.Name, store.Address, store.Phone,
            store.Street, store.ProvinceId, store.WardId,
            store.Province?.Name, store.Ward?.Name));
    }
}
