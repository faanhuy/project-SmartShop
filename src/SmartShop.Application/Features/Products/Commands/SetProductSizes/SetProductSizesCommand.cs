using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Products.Commands.SetProductSizes;

public record SetProductSizesCommand(Guid ProductId, List<Guid> SizeIds) : IRequest<ApiResponse<List<ProductSizeDto>>>;
