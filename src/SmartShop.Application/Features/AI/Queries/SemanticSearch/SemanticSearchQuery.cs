using MediatR;
using SmartShop.Application.Features.AI;

namespace SmartShop.Application.Features.AI.Queries.SemanticSearch;

public record SemanticSearchQuery(string Query, int TopN = 10) : IRequest<IReadOnlyList<SemanticSearchResultDto>>;
