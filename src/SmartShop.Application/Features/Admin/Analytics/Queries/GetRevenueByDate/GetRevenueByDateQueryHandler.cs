using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueByDate;

public class GetRevenueByDateQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetRevenueByDateQuery, IReadOnlyList<RevenueByDateDto>>
{
    public async Task<IReadOnlyList<RevenueByDateDto>> Handle(
        GetRevenueByDateQuery request, CancellationToken cancellationToken)
    {
        var rows = await orderRepository.GetRevenueByDateAsync(request.From, request.To, cancellationToken);

        return rows
            .Select(r => new RevenueByDateDto(r.Date.ToString("yyyy-MM-dd"), r.Revenue, r.OrderCount))
            .ToList()
            .AsReadOnly();
    }
}
