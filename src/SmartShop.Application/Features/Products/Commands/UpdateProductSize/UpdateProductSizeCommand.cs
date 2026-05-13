using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Products.Commands.UpdateProductSize;

public record UpdateProductSizeCommand(
    Guid SizeId,
    string SizeLabel,
    int DisplayOrder) : IRequest<ApiResponse<ProductSizeDto>>;
