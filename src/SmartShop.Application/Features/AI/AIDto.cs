namespace SmartShop.Application.Features.AI;

public record SemanticSearchResultDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    decimal OriginalPrice,
    string Slug,
    string? ImageUrl,
    Guid CategoryId,
    double Score
);
