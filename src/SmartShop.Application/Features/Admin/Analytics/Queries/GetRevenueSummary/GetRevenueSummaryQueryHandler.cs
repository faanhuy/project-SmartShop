using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueSummary;

public class GetRevenueSummaryQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetRevenueSummaryQuery, RevenueSummaryDto>
{
    public async Task<RevenueSummaryDto> Handle(
        GetRevenueSummaryQuery request, CancellationToken cancellationToken)
    {
        var (totalRevenue, totalOrders, averageOrderValue) =
            await orderRepository.GetRevenueSummaryAsync(request.From, request.To, cancellationToken);

        var (prevRevenue, _) =
            await orderRepository.GetPrevPeriodSummaryAsync(request.From, request.To, cancellationToken);

        var newCustomers =
            await orderRepository.GetNewCustomersCountAsync(request.From, request.To, cancellationToken);

        var revenueGrowthPercent = prevRevenue > 0
            ? Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 2)
            : (totalRevenue > 0 ? 100m : 0m);

        return new RevenueSummaryDto(
            totalRevenue,
            totalOrders,
            newCustomers,
            averageOrderValue,
            revenueGrowthPercent);
    }
}
