using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Products.Commands.AddProductSize;

public class AddProductSizeCommandHandler(
    IProductRepository productRepository,
    IProductSizeRepository productSizeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddProductSizeCommand, ApiResponse<ProductSizeDto>>
{
    public async Task<ApiResponse<ProductSizeDto>> Handle(
        AddProductSizeCommand request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SizeLabel);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (!product.HasSizes)
            throw new ConflictException("Sản phẩm này không hỗ trợ phân loại theo size.");

        var existing = await productSizeRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        if (existing.Any(s => s.SizeLabel.Equals(request.SizeLabel, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Size '{request.SizeLabel}' đã tồn tại cho sản phẩm này.");

        var size = ProductSize.Create(request.ProductId, request.SizeLabel, request.DisplayOrder);
        await productSizeRepository.AddAsync(size, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<ProductSizeDto>.Ok(ProductSizeDto.From(size));
    }
}
