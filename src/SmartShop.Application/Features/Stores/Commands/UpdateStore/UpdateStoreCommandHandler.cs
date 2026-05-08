using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Features.Stores.Queries.GetStores;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Stores.Commands.UpdateStore;

public class UpdateStoreCommandHandler(
    IStoreRepository storeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStoreCommand, ApiResponse<StoreDto>>
{
    public async Task<ApiResponse<StoreDto>> Handle(UpdateStoreCommand request, CancellationToken cancellationToken)
    {
        var store = await storeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Store), request.Id);

        store.Update(request.Name, request.Address, request.Phone);

        if (request.IsActive)
            store.Activate();
        else
            store.Deactivate();

        storeRepository.Update(store);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<StoreDto>.Ok(new StoreDto(store.Id, store.Name, store.Address, store.Phone));
    }
}
