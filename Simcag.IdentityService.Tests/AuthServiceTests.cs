using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Simcag.IdentityService.Application.DTOs;
using Simcag.IdentityService.Application.Interfaces;
using Simcag.IdentityService.Application.Services;
using Simcag.IdentityService.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Simcag.IdentityService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _jwtServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnTokens_WhenValidData()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            Name = "Test User",
            Role = "User"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var createdUser = User.Create(request.Email, "hashed-password", request.Name, UserRole.User);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => createdUser = user)
            .Returns(Task.CompletedTask);

        _jwtServiceMock.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        // Act
        var result = await _authService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(request.Email);
        result.User.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "password123",
            Name = "Test User"
        };

        var existingUser = User.Create(request.Email, "hash", request.Name, UserRole.User);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("already exists");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "password123"
        };

        var user = User.Create(request.Email, BCrypt.Net.BCrypt.HashPassword(request.Password), "Test User", UserRole.User);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtServiceMock.Setup(x => x.GenerateAccessTokenAsync(user.Id, user.Email, user.Name, user.Role.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrongpassword"
        };

        var user = User.Create(request.Email, BCrypt.Net.BCrypt.HashPassword("correctpassword"), "Test User", UserRole.User);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid email or password");
        result.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        var user = User.Create("user@example.com", "hash", "Test User", UserRole.User);
        var refreshToken = RefreshToken.Create("valid-refresh-token", user.Id, DateTime.UtcNow.AddDays(1));

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(refreshToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtServiceMock.Setup(x => x.GenerateAccessTokenAsync(user.Id, user.Email, user.Name, user.Role.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-access-token");
        _jwtServiceMock.Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-refresh-token");

        // Act
        var result = await _authService.RefreshTokenAsync(request, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
    }
}