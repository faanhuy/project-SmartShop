using MediatR;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Returns.Queries.GetMyReturnRequests;

public class GetMyReturnRequestsQueryHandler(
    IReturnRequestRepository returnRequestRepository,
    ICurrentUserService currentUserService) : IRequestHandler<GetMyReturnRequestsQuery, List<ReturnRequestDto>>
{
    public async Task<List<ReturnRequestDto>> Handle(
        GetMyReturnRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUserService.UserId);

        var returnRequests = await returnRequestRepository.GetByUserIdAsync(userId, cancellationToken);

        return returnRequests
            .Select(r => ReturnRequestMapper.ToDto(r, r.Order.Id.ToString()))
            .ToList();
    }
}
