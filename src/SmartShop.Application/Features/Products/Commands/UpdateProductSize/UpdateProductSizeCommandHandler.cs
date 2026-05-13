using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Products.Commands.UpdateProductSize;

public class UpdateProductSizeCommandHandler(
    IProductSizeRepository productSizeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductSizeCommand, ApiResponse<ProductSizeDto>>
{
    public async Task<ApiResponse<ProductSizeDto>> Handle(
        UpdateProductSizeCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SizeLabel);

        var size = await productSizeRepository.GetByIdAsync(request.SizeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductSize), request.SizeId);

        var siblings = await productSizeRepository.GetByProductIdAsync(size.ProductId, cancellationToken);
        if (siblings.Any(s => s.Id != request.SizeId &&
                              s.SizeLabel.Equals(request.SizeLabel, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Size '{request.SizeLabel}' đã tồn tại cho sản phẩm này.");

        size.Update(request.SizeLabel, request.DisplayOrder);
        productSizeRepository.Update(size);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductSizeDto>.Ok(ProductSizeDto.From(size));
    }
}
