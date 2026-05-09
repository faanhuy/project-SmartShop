using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Geography.Queries.GetWardsByProvince;

public class GetWardsByProvinceQueryHandler(IWardRepository wardRepository)
    : IRequestHandler<GetWardsByProvinceQuery, ApiResponse<List<WardDto>>>
{
    public async Task<ApiResponse<List<WardDto>>> Handle(GetWardsByProvinceQuery request, CancellationToken cancellationToken)
    {
        var wards = await wardRepository.GetByProvinceAsync(request.ProvinceId);
        var dtos = wards.Select(w => new WardDto(w.Id, w.ProvinceId, w.Name, w.Code)).ToList();
        return ApiResponse<List<WardDto>>.Ok(dtos);
    }
}
