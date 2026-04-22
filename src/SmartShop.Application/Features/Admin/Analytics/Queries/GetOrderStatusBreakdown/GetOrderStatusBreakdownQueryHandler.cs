using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetOrderStatusBreakdown;

public class GetOrderStatusBreakdownQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrderStatusBreakdownQuery, IReadOnlyList<OrderStatusBreakdownDto>>
{
    public async Task<IReadOnlyList<OrderStatusBreakdownDto>> Handle(
        GetOrderStatusBreakdownQuery request, CancellationToken cancellationToken)
    {
        var rows = await orderRepository.GetOrderStatusBreakdownAsync(cancellationToken);

        return rows
            .Select(r => new OrderStatusBreakdownDto(r.Status, r.Count))
            .ToList()
            .AsReadOnly();
    }
}
