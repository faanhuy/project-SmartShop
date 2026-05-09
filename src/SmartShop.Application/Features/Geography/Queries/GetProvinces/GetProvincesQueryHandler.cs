using MediatR;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Features.Geography.Queries.GetProvinces;

public class GetProvincesQueryHandler(IProvinceRepository provinceRepository)
    : IRequestHandler<GetProvincesQuery, ApiResponse<List<ProvinceDto>>>
{
    public async Task<ApiResponse<List<ProvinceDto>>> Handle(GetProvincesQuery request, CancellationToken cancellationToken)
    {
        var provinces = await provinceRepository.GetAllAsync();
        var dtos = provinces.Select(p => new ProvinceDto(p.Id, p.Name, p.Code)).ToList();
        return ApiResponse<List<ProvinceDto>>.Ok(dtos);
    }
}
