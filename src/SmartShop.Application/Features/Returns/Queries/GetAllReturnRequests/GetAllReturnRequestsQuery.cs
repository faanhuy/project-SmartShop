using MediatR;
using SmartShop.Domain.Enums;

namespace SmartShop.Application.Features.Returns.Queries.GetAllReturnRequests;

public record GetAllReturnRequestsQuery(ReturnStatus? Status) : IRequest<List<ReturnRequestDto>>;
