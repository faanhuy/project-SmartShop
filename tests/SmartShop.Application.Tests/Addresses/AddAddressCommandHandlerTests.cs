using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Addresses.Commands.AddAddress;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Addresses;

public class AddAddressCommandHandlerTests
{
    private readonly Mock<IUserAddressRepository> _addressRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AddAddressCommandHandler CreateHandler() =>
        new(_addressRepo.Object, _uow.Object);

    private static AddAddressCommand ValidCommand(string userId) =>
        new(userId, "Nhà riêng", "Nguyen Van A", "0901234567",
            "123 Đường Lê Lợi", "Phường Bến Nghé", "Quận 1", "TP.HCM");

    [Fact]
    public async Task AddAddress_ValidRequest_ReturnsAddressDto()
    {
        var userId = Guid.NewGuid().ToString();
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress>());
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Label.Should().Be("Nhà riêng");
        result.Data.RecipientName.Should().Be("Nguyen Van A");
        result.Data.City.Should().Be("TP.HCM");
    }

    [Fact]
    public async Task AddAddress_FirstAddress_AutoSetsIsDefaultTrue()
    {
        var userId = Guid.NewGuid().ToString();
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress>());
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Data!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task AddAddress_SecondAddress_NotDefault()
    {
        var userId = Guid.NewGuid().ToString();
        var existing = UserAddress.Create(userId, "Văn phòng", "Nguyen Van A",
            "0901234567", "456 Đường CMT8", null, "Quận 3", "TP.HCM");
        existing.SetAsDefault();

        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress> { existing });
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(ValidCommand(userId), default);

        result.Data!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task AddAddress_CallsRepository_Once()
    {
        var userId = Guid.NewGuid().ToString();
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress>());
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(ValidCommand(userId), default);

        _addressRepo.Verify(r => r.AddAsync(It.IsAny<UserAddress>(), default), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddAddress_WithProvinceIdAndWardId_SavesThem()
    {
        var userId = Guid.NewGuid().ToString();
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress>());
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new AddAddressCommand(userId, "Nhà riêng", "Nguyen Van A", "0901234567",
            "123 Đường Lê Lợi", "Phường Bến Nghé", "Quận 1", "TP.HCM",
            ProvinceId: 1, WardId: 1001);

        var result = await CreateHandler().Handle(command, default);

        result.Data.Should().NotBeNull();
        result.Data!.ProvinceId.Should().Be(1);
        result.Data.WardId.Should().Be(1001);
    }
}
