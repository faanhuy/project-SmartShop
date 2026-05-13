using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Products.Commands.AddProductSize;

public record AddProductSizeCommand(
    Guid ProductId,
    string SizeLabel,
    int DisplayOrder) : IRequest<ApiResponse<ProductSizeDto>>;
