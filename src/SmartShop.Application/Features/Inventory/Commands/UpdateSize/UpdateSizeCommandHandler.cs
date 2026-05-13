using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.UpdateSize;

public class UpdateSizeCommandHandler(ISizeRepository repo, IUnitOfWork uow)
    : IRequestHandler<UpdateSizeCommand, ApiResponse<SizeDto>>
{
    public async Task<ApiResponse<SizeDto>> Handle(UpdateSizeCommand request, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Label);

        var size = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Size), request.Id);

        size.Update(request.Label, request.DisplayOrder);
        repo.Update(size);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<SizeDto>.Ok(SizeDto.From(size));
    }
}
