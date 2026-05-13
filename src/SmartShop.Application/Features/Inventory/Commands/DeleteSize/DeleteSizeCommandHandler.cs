using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.DeleteSize;

public class DeleteSizeCommandHandler(ISizeRepository repo, IUnitOfWork uow)
    : IRequestHandler<DeleteSizeCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(DeleteSizeCommand request, CancellationToken ct)
    {
        var size = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Size), request.Id);

        size.Deactivate();
        repo.Update(size);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<object>.Ok(new { message = "Size đã bị vô hiệu hóa." });
    }
}
