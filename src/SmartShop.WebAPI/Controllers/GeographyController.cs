using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartShop.Application.Features.Geography.Queries.GetProvinces;
using SmartShop.Application.Features.Geography.Queries.GetWardsByProvince;

namespace SmartShop.WebAPI.Controllers;

[ApiController]
[Route("api/geography")]
public class GeographyController(IMediator mediator) : ControllerBase
{
    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces() =>
        Ok(await mediator.Send(new GetProvincesQuery()));

    [HttpGet("provinces/{provinceId}/wards")]
    public async Task<IActionResult> GetWards(int provinceId) =>
        Ok(await mediator.Send(new GetWardsByProvinceQuery(provinceId)));
}
