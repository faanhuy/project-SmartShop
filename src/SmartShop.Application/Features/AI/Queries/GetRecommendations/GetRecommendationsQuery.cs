using MediatR;
using SmartShop.Application.DTOs;

namespace SmartShop.Application.Features.AI.Queries.GetRecommendations;

public record GetRecommendationsQuery(Guid ProductId, int Count = 5) : IRequest<IReadOnlyList<ProductDto>>;
