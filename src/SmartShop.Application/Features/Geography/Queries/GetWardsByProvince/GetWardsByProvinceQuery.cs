using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Geography.Queries.GetWardsByProvince;

public record GetWardsByProvinceQuery(int ProvinceId) : IRequest<ApiResponse<List<WardDto>>>;

public record WardDto(int Id, int ProvinceId, string Name, string Code);
