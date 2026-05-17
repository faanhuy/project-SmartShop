using MediatR;

namespace SmartShop.Application.Features.Returns.Queries.GetMyReturnRequests;

public record GetMyReturnRequestsQuery : IRequest<List<ReturnRequestDto>>;
