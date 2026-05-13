using MediatR;
using SmartShop.Application.DTOs;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    Guid CategoryId,
    string Slug,
    string? ImageUrl = null,
    decimal? OriginalPrice = null,
    bool HasSizes = false,
    SizeType? SizeType = null
) : IRequest<ProductDto>;
