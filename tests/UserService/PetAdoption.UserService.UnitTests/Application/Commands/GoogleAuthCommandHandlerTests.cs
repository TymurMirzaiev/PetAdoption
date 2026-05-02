namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using Microsoft.Extensions.Options;
using PetAdoption.UserService.Application.Options;
using PetAdoption.UserService.Domain.ValueObjects;

public class GoogleAuthCommandHandlerTests
{
    private readonly Mock<IGoogleTokenValidator> _mockGoogleValidator;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly GoogleAuthCommandHandler _handler;

    public GoogleAuthCommandHandlerTests()
    {
        _mockGoogleValidator = new Mock<IGoogleTokenValidator>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _handler = new GoogleAuthCommandHandler(
            _mockGoogleValidator.Object,
            _mockUserRepo.Object,
            _mockJwtGenerator.Object,
            _mockRefreshTokenRepo.Object,
            Options.Create(new JwtOptions()));
    }

    // ──────────────────────────────────────────────────────────────
    // Existing user
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ExistingGoogleUser_ShouldReturnTokens()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("test@gmail.com", "Test User");
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(googleInfo);

        var existingUser = User.RegisterFromGoogle("test@gmail.com", "Test User");
        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(existingUser);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        var result = await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        _mockUserRepo.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingGoogleUser_ShouldRecordLogin()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("test@gmail.com", "Test User");
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(googleInfo);

        var existingUser = User.RegisterFromGoogle("test@gmail.com", "Test User");
        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(existingUser);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        _mockUserRepo.Verify(r => r.SaveAsync(existingUser), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // New user (auto-register)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NewGoogleUser_ShouldAutoRegisterAndReturnTokens()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("new@gmail.com", "New User");
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(googleInfo);

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        var result = await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        _mockUserRepo.Verify(
            r => r.SaveAsync(It.Is<User>(u => u.ExternalProvider == "Google")),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Invalid token
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_InvalidGoogleToken_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("bad-token"))
            .ReturnsAsync((GoogleUserInfo?)null);

        // Act & Assert
        var act = () => _handler.HandleAsync(new GoogleAuthCommand("bad-token"));
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Suspended user
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SuspendedUser_ShouldThrowUserSuspendedException()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("suspended@gmail.com", "Suspended User");
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(googleInfo);

        var suspendedUser = User.RegisterFromGoogle("suspended@gmail.com", "Suspended User");
        suspendedUser.Suspend("Policy violation");
        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(suspendedUser);

        // Act & Assert
        var act = () => _handler.HandleAsync(new GoogleAuthCommand("valid-token"));
        await act.Should().ThrowAsync<UserSuspendedException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Refresh token creation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidToken_ShouldCreateRefreshToken()
    {
        // Arrange
        var googleInfo = new GoogleUserInfo("test@gmail.com", "Test User");
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid-token"))
            .ReturnsAsync(googleInfo);

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt-token");

        // Act
        await _handler.HandleAsync(new GoogleAuthCommand("valid-token"));

        // Assert
        _mockRefreshTokenRepo.Verify(
            r => r.SaveAsync(It.IsAny<RefreshToken>()),
            Times.Once);
    }
}
