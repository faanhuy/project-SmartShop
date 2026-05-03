using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Addresses.Commands.SetDefaultAddress;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Addresses;

public class SetDefaultAddressCommandHandlerTests
{
    private readonly Mock<IUserAddressRepository> _addressRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private SetDefaultAddressCommandHandler CreateHandler() =>
        new(_addressRepo.Object, _uow.Object);

    private static UserAddress CreateAddress(string userId, bool isDefault = false)
    {
        var addr = UserAddress.Create(userId, "Label", "Nguyen Van A",
            "0901234567", "123 Đường Lê Lợi", null, "Quận 1", "TP.HCM");
        if (isDefault)
            addr.SetAsDefault();
        return addr;
    }

    [Fact]
    public async Task SetDefault_ValidAddress_UnsetsAllOthers()
    {
        var userId = Guid.NewGuid().ToString();
        var address1 = CreateAddress(userId, isDefault: true);
        var address2 = CreateAddress(userId, isDefault: false);
        var address3 = CreateAddress(userId, isDefault: false);

        _addressRepo.Setup(r => r.GetByIdAsync(address2.Id, default))
                    .ReturnsAsync(address2);
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(new List<UserAddress> { address1, address2, address3 });
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(new SetDefaultAddressCommand(address2.Id, userId), default);

        result.IsSuccess.Should().BeTrue();
        address2.IsDefault.Should().BeTrue();
        address1.IsDefault.Should().BeFalse();
        address3.IsDefault.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task SetDefault_NotFound_ThrowsNotFoundException()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        _addressRepo.Setup(r => r.GetByIdAsync(addressId, default))
                    .ReturnsAsync((UserAddress?)null);

        var act = () => CreateHandler().Handle(new SetDefaultAddressCommand(addressId, userId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetDefault_WrongOwner_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var address = CreateAddress(otherUserId);

        _addressRepo.Setup(r => r.GetByIdAsync(address.Id, default))
                    .ReturnsAsync(address);

        var act = () => CreateHandler().Handle(new SetDefaultAddressCommand(address.Id, userId), default);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
