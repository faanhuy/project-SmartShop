using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetTopProducts;

public class GetTopProductsQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetTopProductsQuery, IReadOnlyList<TopProductDto>>
{
    public async Task<IReadOnlyList<TopProductDto>> Handle(
        GetTopProductsQuery request, CancellationToken cancellationToken)
    {
        var rows = await orderRepository.GetTopProductsAsync(
            request.From, request.To, request.Limit, cancellationToken);

        return rows
            .Select(r => new TopProductDto(r.ProductId, r.ProductName, r.TotalQuantity, r.TotalRevenue))
            .ToList()
            .AsReadOnly();
    }
}
