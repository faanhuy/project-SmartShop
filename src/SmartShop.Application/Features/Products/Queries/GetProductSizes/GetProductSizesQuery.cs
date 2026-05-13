using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Products.Queries.GetProductSizes;

public record GetProductSizesQuery(Guid ProductId) : IRequest<ApiResponse<List<ProductSizeDto>>>;
