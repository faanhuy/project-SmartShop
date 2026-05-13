using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Inventory.Queries.GetSizes;

public record GetSizesQuery(SizeType? Category = null) : IRequest<ApiResponse<List<SizeDto>>>;
