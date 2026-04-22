using MediatR;
using SmartShop.Application.Features.Admin.Analytics.DTOs;

namespace SmartShop.Application.Features.Admin.Analytics.Queries.GetRevenueSummary;

public record GetRevenueSummaryQuery(DateTime From, DateTime To) : IRequest<RevenueSummaryDto>;
