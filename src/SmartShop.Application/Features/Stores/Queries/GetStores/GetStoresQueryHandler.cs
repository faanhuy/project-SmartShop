using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Stores.Queries.GetStores;

public class GetStoresQueryHandler(IStoreRepository storeRepository)
    : IRequestHandler<GetStoresQuery, ApiResponse<List<StoreDto>>>
{
    public async Task<ApiResponse<List<StoreDto>>> Handle(GetStoresQuery request, CancellationToken cancellationToken)
    {
        var stores = await storeRepository.GetAllActiveAsync(cancellationToken);

        var dtos = stores.Select(s => new StoreDto(s.Id, s.Name, s.Address, s.Phone)).ToList();

        return ApiResponse<List<StoreDto>>.Ok(dtos);
    }
}
