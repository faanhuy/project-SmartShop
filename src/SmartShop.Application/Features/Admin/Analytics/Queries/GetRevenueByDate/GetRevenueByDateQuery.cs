using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueByDate;

public record GetRevenueByDateQuery(DateTime From, DateTime To) : IRequest<IReadOnlyList<RevenueByDateDto>>;
