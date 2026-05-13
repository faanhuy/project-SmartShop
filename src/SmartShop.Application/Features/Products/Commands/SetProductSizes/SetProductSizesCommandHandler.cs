using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Products.Commands.SetProductSizes;

public class SetProductSizesCommandHandler(
    IProductRepository productRepository,
    IProductSizeRepository productSizeRepository,
    ISizeRepository sizeRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SetProductSizesCommand, ApiResponse<List<ProductSizeDto>>>
{
    public async Task<ApiResponse<List<ProductSizeDto>>> Handle(
        SetProductSizesCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        if (!product.HasSizes)
            throw new ConflictException("Sản phẩm này không hỗ trợ phân loại theo kích cỡ.");

        var existing = await productSizeRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        foreach (var ps in existing)
            productSizeRepository.Delete(ps);

        var result = new List<ProductSize>();
        foreach (var sizeId in request.SizeIds)
        {
            var master = await sizeRepository.GetByIdAsync(sizeId, cancellationToken)
                ?? throw new NotFoundException(nameof(Size), sizeId);

            var ps = ProductSize.CreateFromMaster(request.ProductId, sizeId, master.Label, master.DisplayOrder);
            await productSizeRepository.AddAsync(ps, cancellationToken);
            result.Add(ps);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<List<ProductSizeDto>>.Ok(result.Select(ProductSizeDto.From).ToList());
    }
}
