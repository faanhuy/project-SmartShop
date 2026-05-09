using MediatR;
using SmartShop.Application.Common.Models;

namespace SmartShop.Application.Features.Geography.Queries.GetProvinces;

public record GetProvincesQuery : IRequest<ApiResponse<List<ProvinceDto>>>;

public record ProvinceDto(int Id, string Name, string Code);
