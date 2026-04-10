using FluentAssertions;
using Moq;
using SmartShop.Application.Common.Exceptions;
using Xunit;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Features.Auth.Commands.Register;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Entities;
using SmartShop.Domain.Interfaces;

namespace SmartShop.Application.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepo.Object, _hasher.Object, _jwt.Object, _uow.Object);

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndReturnsAuthResponse()
    {
        _userRepo.Setup(r => r.ExistsAsync("new@test.com", default)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("password")).Returns("hashed");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await CreateHandler().Handle(
            new RegisterCommand("new@test.com", "password", "Jane", "Doe"), default);

        result.Token.Should().Be("jwt-token");
        result.Email.Should().Be("new@test.com");
        result.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        _userRepo.Setup(r => r.ExistsAsync("exists@test.com", default)).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(
            new RegisterCommand("exists@test.com", "password", "Jane", "Doe"), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_NewEmail_SavesChangesTwice()
    {
        _userRepo.Setup(r => r.ExistsAsync("new@test.com", default)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("password")).Returns("hashed");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(
            new RegisterCommand("new@test.com", "password", "Jane", "Doe"), default);

        _uow.Verify(u => u.SaveChangesAsync(default), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_NewEmail_HashesPassword()
    {
        _userRepo.Setup(r => r.ExistsAsync("new@test.com", default)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("mypassword")).Returns("hashed");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await CreateHandler().Handle(
            new RegisterCommand("new@test.com", "mypassword", "Jane", "Doe"), default);

        _hasher.Verify(h => h.Hash("mypassword"), Times.Once);
    }
}
