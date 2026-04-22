using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetOrderStatusBreakdown;

public record GetOrderStatusBreakdownQuery() : IRequest<IReadOnlyList<OrderStatusBreakdownDto>>;
