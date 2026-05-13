using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Products.Commands.DeleteProductSize;

public class DeleteProductSizeCommandHandler(
    IProductSizeRepository productSizeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductSizeCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteProductSizeCommand request, CancellationToken cancellationToken)
    {
        var size = await productSizeRepository.GetByIdAsync(request.SizeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductSize), request.SizeId);

        var hasInventory = await productSizeRepository.HasInventoryAsync(request.SizeId, cancellationToken);
        if (hasInventory)
            throw new ConflictException("Không thể xóa size đang có tồn kho. Vui lòng set tồn kho về 0 trước.");

        productSizeRepository.Delete(size);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.Ok(true);
    }
}
