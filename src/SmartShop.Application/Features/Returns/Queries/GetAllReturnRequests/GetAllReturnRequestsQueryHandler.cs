using MediatR;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Returns.Queries.GetAllReturnRequests;

public class GetAllReturnRequestsQueryHandler(
    IReturnRequestRepository returnRequestRepository) : IRequestHandler<GetAllReturnRequestsQuery, List<ReturnRequestDto>>
{
    public async Task<List<ReturnRequestDto>> Handle(
        GetAllReturnRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var returnRequests = await returnRequestRepository.GetAllAsync(request.Status, cancellationToken);

        return returnRequests
            .Select(r => ReturnRequestMapper.ToDto(r, r.Order.Id.ToString()))
            .ToList();
    }
}
