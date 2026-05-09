using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Geography.Queries.GetWardsByProvince;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Geography;

public class GetWardsByProvinceQueryHandlerTests
{
    private readonly Mock<IWardRepository> _wardRepo = new();

    private GetWardsByProvinceQueryHandler CreateHandler() =>
        new(_wardRepo.Object);

    [Fact]
    public async Task Handle_ReturnsWards_ForGivenProvince()
    {
        const int provinceId = 1;
        var wards = new List<Ward>
        {
            Ward.Create(1001, provinceId, "Phường Bến Nghé", "ben-nghe"),
            Ward.Create(1002, provinceId, "Phường Bến Thành", "ben-thanh"),
            Ward.Create(1003, provinceId, "Phường Cầu Ông Lãnh", "cau-ong-lanh")
        };
        _wardRepo.Setup(r => r.GetByProvinceAsync(provinceId)).ReturnsAsync(wards);

        var result = await CreateHandler().Handle(new GetWardsByProvinceQuery(provinceId), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(3);
        result.Data.Should().AllSatisfy(w => w.ProvinceId.Should().Be(provinceId));
    }

    [Fact]
    public async Task Handle_EmptyProvince_ReturnsEmptyList()
    {
        const int provinceId = 99;
        _wardRepo.Setup(r => r.GetByProvinceAsync(provinceId)).ReturnsAsync(new List<Ward>());

        var result = await CreateHandler().Handle(new GetWardsByProvinceQuery(provinceId), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsWardDto_Correctly()
    {
        const int provinceId = 1;
        var wards = new List<Ward>
        {
            Ward.Create(1001, provinceId, "Phường Bến Nghé", "ben-nghe")
        };
        _wardRepo.Setup(r => r.GetByProvinceAsync(provinceId)).ReturnsAsync(wards);

        var result = await CreateHandler().Handle(new GetWardsByProvinceQuery(provinceId), default);

        var dto = result.Data!.Single();
        dto.Id.Should().Be(1001);
        dto.ProvinceId.Should().Be(provinceId);
        dto.Name.Should().Be("Phường Bến Nghé");
        dto.Code.Should().Be("ben-nghe");
    }
}
