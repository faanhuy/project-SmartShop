using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Commands.CreateSize;

public class CreateSizeCommandHandler(ISizeRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateSizeCommand, ApiResponse<SizeDto>>
{
    public async Task<ApiResponse<SizeDto>> Handle(CreateSizeCommand request, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Label);

        // Check for duplicate
        var exists = await repo.ExistsByLabelAndCategoryAsync(request.Label, request.Category, ct);
        if (exists)
            throw new InvalidOperationException($"Size với tên '{request.Label}' đã tồn tại trong category này.");

        var size = Size.Create(request.Category, request.Label, request.DisplayOrder);
        await repo.AddAsync(size, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<SizeDto>.Ok(SizeDto.From(size));
    }
}
