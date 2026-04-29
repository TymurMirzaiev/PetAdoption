namespace PetAdoption.UserService.Tests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _handler = new RefreshTokenCommandHandler(
            _mockRefreshTokenRepo.Object,
            _mockUserRepo.Object,
            _mockJwtGenerator.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldReturnNewTokenPair()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        var user = CreateTestUser("user-123");

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new-access-token");

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(existingToken.Token);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldRevokeOldToken()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        var user = CreateTestUser("user-123");

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new-access-token");

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        existingToken.IsRevoked.Should().BeTrue();
        _mockRefreshTokenRepo.Verify(r => r.SaveAsync(existingToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ShouldSaveNewRefreshToken()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        var user = CreateTestUser("user-123");

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        _mockJwtGenerator
            .Setup(g => g.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("new-access-token");

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockRefreshTokenRepo.Verify(
            r => r.SaveAsync(It.Is<RefreshToken>(t => t.Token != existingToken.Token)),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Errors
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithNonExistentToken_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync("bad-token"))
            .ReturnsAsync((RefreshToken?)null);

        var command = new RefreshTokenCommand("bad-token");

        // Act & Assert
        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_WithRevokedToken_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));
        existingToken.Revoke();

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act & Assert
        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var existingToken = RefreshToken.Create("user-123", TimeSpan.FromDays(30));

        _mockRefreshTokenRepo
            .Setup(r => r.GetByTokenAsync(existingToken.Token))
            .ReturnsAsync(existingToken);

        _mockUserRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        var command = new RefreshTokenCommand(existingToken.Token);

        // Act & Assert
        var act = () => _handler.HandleAsync(command);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static User CreateTestUser(string userId)
    {
        var user = User.Register("test@example.com", "Test User", "$2a$12$hashedpassword");
        typeof(User).GetProperty("Id")!.SetValue(user, UserId.From(userId));
        return user;
    }
}
