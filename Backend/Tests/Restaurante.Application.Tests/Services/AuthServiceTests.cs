using FluentAssertions;
using Moq;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Application.Services;
using Restaurante.Domain.Entities;
using Restaurante.Domain.Interfaces;

namespace Restaurante.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    public AuthServiceTests()
    {
        _uow.Setup(u => u.Users).Returns(_userRepo.Object);
    }

    private AuthService BuildSut() => new(_uow.Object, _hasher.Object, _jwt.Object);

    [Fact]
    public async Task Login_Returns_Null_When_Email_Not_Found()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("nope@x.com")).ReturnsAsync((User?)null);

        var result = await BuildSut().LoginAsync(new LoginRequest("nope@x.com", "any"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_Returns_Null_When_Password_Is_Wrong()
    {
        var user = new User { Id = 1, Email = "a@b.com", PasswordHash = "hash", Role = UserRoles.User };
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrong", "hash")).Returns(false);

        var result = await BuildSut().LoginAsync(new LoginRequest(user.Email, "wrong"));

        result.Should().BeNull();
        _jwt.Verify(j => j.GenerateToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_Returns_Token_And_Role_On_Success()
    {
        var user = new User { Id = 42, Name = "Admin", Email = "admin@x.com", PasswordHash = "h", Role = UserRoles.Admin };
        _userRepo.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("pwd", "h")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(42, user.Email, UserRoles.Admin)).Returns("jwt.token");

        var result = await BuildSut().LoginAsync(new LoginRequest(user.Email, "pwd"));

        result.Should().NotBeNull();
        result!.Token.Should().Be("jwt.token");
        result.Role.Should().Be(UserRoles.Admin);
        result.UserId.Should().Be(42);
    }

    [Fact]
    public async Task Register_Returns_Null_When_Email_Already_Exists()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("dup@x.com")).ReturnsAsync(true);

        var result = await BuildSut().RegisterAsync(new RegisterRequest("X", "dup@x.com", "secret1"));

        result.Should().BeNull();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_Creates_User_With_Hashed_Password_And_Default_Role()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("new@x.com")).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("secret1")).Returns("hashed");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<int>(), "new@x.com", UserRoles.User)).Returns("tk");

        User? captured = null;
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>()))
                 .Callback<User>(u => captured = u)
                 .Returns(Task.CompletedTask);

        var result = await BuildSut().RegisterAsync(new RegisterRequest("Nuevo", "new@x.com", "secret1"));

        result.Should().NotBeNull();
        result!.Role.Should().Be(UserRoles.User);
        captured!.PasswordHash.Should().Be("hashed", "el password nunca debe guardarse en claro");
        captured.Role.Should().Be(UserRoles.User);
    }
}
