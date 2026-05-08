namespace SmartShop.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    decimal OriginalPrice,
    string Slug,
    string? ImageUrl,
    bool IsActive,
    Guid CategoryId,
    DateTime CreatedAt
);
