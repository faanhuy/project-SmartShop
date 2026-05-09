using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Geography.Queries.GetProvinces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Geography;

public class GetProvincesQueryHandlerTests
{
    private readonly Mock<IProvinceRepository> _provinceRepo = new();

    private GetProvincesQueryHandler CreateHandler() =>
        new(_provinceRepo.Object);

    [Fact]
    public async Task Handle_ReturnsAllProvinces_MappedToDto()
    {
        var provinces = new List<Province>
        {
            Province.Create(1, "Hà Nội", "ha-noi"),
            Province.Create(2, "TP. Hồ Chí Minh", "tp-hcm")
        };
        _provinceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(provinces);

        var result = await CreateHandler().Handle(new GetProvincesQuery(), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(2);
        result.Data[0].Id.Should().Be(1);
        result.Data[0].Name.Should().Be("Hà Nội");
        result.Data[0].Code.Should().Be("ha-noi");
        result.Data[1].Id.Should().Be(2);
        result.Data[1].Name.Should().Be("TP. Hồ Chí Minh");
        result.Data[1].Code.Should().Be("tp-hcm");
    }

    [Fact]
    public async Task Handle_EmptyRepo_ReturnsEmptyList()
    {
        _provinceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Province>());

        var result = await CreateHandler().Handle(new GetProvincesQuery(), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_MapsIdNameCode_Correctly()
    {
        var provinces = new List<Province>
        {
            Province.Create(1, "Hà Nội", "ha-noi")
        };
        _provinceRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(provinces);

        var result = await CreateHandler().Handle(new GetProvincesQuery(), default);

        var dto = result.Data!.Single();
        dto.Id.Should().Be(1);
        dto.Name.Should().Be("Hà Nội");
        dto.Code.Should().Be("ha-noi");
    }
}
