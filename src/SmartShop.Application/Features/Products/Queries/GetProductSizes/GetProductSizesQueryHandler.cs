using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Products.Queries.GetProductSizes;

public class GetProductSizesQueryHandler(IProductSizeRepository productSizeRepository)
    : IRequestHandler<GetProductSizesQuery, ApiResponse<List<ProductSizeDto>>>
{
    public async Task<ApiResponse<List<ProductSizeDto>>> Handle(
        GetProductSizesQuery request, CancellationToken cancellationToken)
    {
        var sizes = await productSizeRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        var dtos = sizes.Select(ProductSizeDto.From).ToList();
        return ApiResponse<List<ProductSizeDto>>.Ok(dtos);
    }
}
