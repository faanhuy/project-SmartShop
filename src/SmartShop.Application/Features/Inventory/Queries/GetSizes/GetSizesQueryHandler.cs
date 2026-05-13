using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Inventory.Queries.GetSizes;

public class GetSizesQueryHandler(ISizeRepository repo) : IRequestHandler<GetSizesQuery, ApiResponse<List<SizeDto>>>
{
    public async Task<ApiResponse<List<SizeDto>>> Handle(GetSizesQuery request, CancellationToken cancellationToken)
    {
        List<SizeDto> sizes;

        if (request.Category.HasValue)
        {
            var sizeEntities = await repo.GetByCategoryAsync(request.Category.Value, cancellationToken);
            sizes = sizeEntities.Select(SizeDto.From).ToList();
        }
        else
        {
            var sizeEntities = await repo.GetAllAsync(cancellationToken);
            sizes = sizeEntities.Select(SizeDto.From).ToList();
        }

        return ApiResponse<List<SizeDto>>.Ok(sizes);
    }
}
