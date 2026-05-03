using FluentAssertions;
using Moq;
using Xunit;
using SmartShop.Application.Features.Addresses.Commands.DeleteAddress;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Common.Exceptions;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Addresses;

public class DeleteAddressCommandHandlerTests
{
    private readonly Mock<IUserAddressRepository> _addressRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DeleteAddressCommandHandler CreateHandler() =>
        new(_addressRepo.Object, _uow.Object);

    private static UserAddress CreateAddress(string userId, bool isDefault = false)
    {
        var addr = UserAddress.Create(userId, "Nhà riêng", "Nguyen Van A",
            "0901234567", "123 Đường Lê Lợi", null, "Quận 1", "TP.HCM");
        if (isDefault)
            addr.SetAsDefault();
        return addr;
    }

    [Fact]
    public async Task DeleteAddress_NotFound_ThrowsNotFoundException()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();

        _addressRepo.Setup(r => r.GetByIdAsync(addressId, default))
                    .ReturnsAsync((UserAddress?)null);

        var act = () => CreateHandler().Handle(new DeleteAddressCommand(addressId, userId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAddress_WrongOwner_ThrowsUnauthorizedException()
    {
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var address = CreateAddress(otherUserId);

        _addressRepo.Setup(r => r.GetByIdAsync(address.Id, default))
                    .ReturnsAsync(address);

        var act = () => CreateHandler().Handle(new DeleteAddressCommand(address.Id, userId), default);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task DeleteAddress_DefaultAddress_PromotesOldestRemaining()
    {
        var userId = Guid.NewGuid().ToString();
        var defaultAddress = CreateAddress(userId, isDefault: true);

        // oldest: created earlier
        var oldestAddress = UserAddress.Create(userId, "Cũ nhất", "Nguyen Van B",
            "0909999999", "789 Đường Nguyễn Huệ", null, "Quận 1", "TP.HCM");
        oldestAddress.CreatedAt = DateTime.UtcNow.AddDays(-5);

        var newerAddress = UserAddress.Create(userId, "Mới hơn", "Nguyen Van C",
            "0908888888", "101 Đường Pasteur", null, "Quận 3", "TP.HCM");
        newerAddress.CreatedAt = DateTime.UtcNow.AddDays(-1);

        var remaining = new List<UserAddress> { defaultAddress, oldestAddress, newerAddress };

        _addressRepo.Setup(r => r.GetByIdAsync(defaultAddress.Id, default))
                    .ReturnsAsync(defaultAddress);
        _addressRepo.Setup(r => r.GetByUserIdAsync(userId, default))
                    .ReturnsAsync(remaining);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteAddressCommand(defaultAddress.Id, userId), default);

        oldestAddress.IsDefault.Should().BeTrue();
        newerAddress.IsDefault.Should().BeFalse();
        _addressRepo.Verify(r => r.Remove(defaultAddress), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAddress_NonDefaultAddress_NoPromotion()
    {
        var userId = Guid.NewGuid().ToString();
        var nonDefaultAddress = CreateAddress(userId, isDefault: false);
        var otherAddress = CreateAddress(userId, isDefault: true);

        _addressRepo.Setup(r => r.GetByIdAsync(nonDefaultAddress.Id, default))
                    .ReturnsAsync(nonDefaultAddress);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(new DeleteAddressCommand(nonDefaultAddress.Id, userId), default);

        // GetByUserIdAsync should NOT be called because address is not default
        _addressRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<string>(), default), Times.Never);
        // otherAddress remains the default — untouched
        otherAddress.IsDefault.Should().BeTrue();
        _addressRepo.Verify(r => r.Remove(nonDefaultAddress), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
