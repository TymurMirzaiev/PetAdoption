namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using Microsoft.Extensions.Options;
using PetAdoption.UserService.Application.Options;
using PetAdoption.UserService.Domain.ValueObjects;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _handler = new LoginCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object,
            _mockRefreshTokenRepo.Object,
            Options.Create(new JwtOptions())
        );
    }

    // ──────────────────────────────────────────────────────────────
    // Success Cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ShouldReturnLoginResponse()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "SecurePass123!");
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("SecurePass123!", "$2a$12$hashedpassword"))
            .Returns(true);

        _mockJwtTokenGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt.token.here");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Token.Should().Be("jwt.token.here");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("John Doe");
        result.Role.Should().Be("User");
        result.ExpiresIn.Should().Be(3600);

        _mockUserRepository.Verify(r => r.SaveAsync(user), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Error Cases (invalid credentials, suspended)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithNonExistentUser_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("nonexistent@example.com", "password");

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "WrongPassword123!");
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword("WrongPassword123!", "$2a$12$hashedpassword"))
            .Returns(false);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _mockJwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithSuspendedUser_ShouldThrowUserSuspendedException()
    {
        // Arrange
        var command = new LoginCommand("suspended@example.com", "SecurePass123!");
        var user = User.Register(
            "suspended@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );
        user.Suspend("Account suspended for policy violation");

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UserSuspendedException>();
        _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────
    // Side Effects (token, login recording)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ShouldGenerateJwtToken()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "SecurePass123!");
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _mockJwtTokenGenerator
            .Setup(g => g.GenerateToken(user.Id.Value, "test@example.com", "User"))
            .Returns("jwt.token.here");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockJwtTokenGenerator.Verify(
            g => g.GenerateToken(user.Id.Value, "test@example.com", "User"),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldRecordLogin()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", "SecurePass123!");
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<Email>()))
            .ReturnsAsync(user);

        _mockPasswordHasher
            .Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _mockJwtTokenGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("jwt.token.here");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(r => r.SaveAsync(user), Times.Once);
    }
}
