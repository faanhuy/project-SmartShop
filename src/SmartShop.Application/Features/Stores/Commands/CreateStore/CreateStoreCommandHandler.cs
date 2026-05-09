using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStoreCommand, ApiResponse<StoreDto>>
{
    public async Task<ApiResponse<StoreDto>> Handle(CreateStoreCommand request, CancellationToken cancellationToken)
    {
        var store = Store.Create(request.Name, request.Address, request.Phone,
            request.ProvinceId, request.WardId, request.Street);

        await storeRepository.AddAsync(store, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<StoreDto>.Ok(new StoreDto(
            store.Id, store.Name, store.Address, store.Phone,
            store.Street, store.ProvinceId, store.WardId));
    }
}
