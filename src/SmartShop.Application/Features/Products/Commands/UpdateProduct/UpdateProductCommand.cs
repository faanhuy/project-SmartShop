using MediatR;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string? ImageUrl,
    decimal? OriginalPrice = null,
    bool HasSizes = false,
    SizeType? SizeType = null
) : IRequest<ProductDto>;
