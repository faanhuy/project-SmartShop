using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetTopProducts;

public record GetTopProductsQuery(DateTime From, DateTime To, int Limit = 5)
    : IRequest<IReadOnlyList<TopProductDto>>;
