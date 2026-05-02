namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly UpdateUserProfileCommandHandler _handler;

    public UpdateUserProfileCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _handler = new UpdateUserProfileCommandHandler(_mockUserRepository.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success Cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            "+1234567890"
        );

        var command = new UpdateUserProfileCommand(
            user.Id.Value,
            "Jane Doe",
            "+9876543210",
            new UpdatePreferencesDto(
                PreferredPetType: "Cat",
                PreferredSizes: null,
                PreferredAgeRange: null,
                ReceiveEmailNotifications: false,
                ReceiveSmsNotifications: false)
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Profile updated successfully");

        _mockUserRepository.Verify(r => r.SaveAsync(user), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallUpdateProfile()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var preferences = new UpdatePreferencesDto(
            PreferredPetType: "Dog",
            PreferredSizes: null,
            PreferredAgeRange: null,
            ReceiveEmailNotifications: true,
            ReceiveSmsNotifications: false);

        var command = new UpdateUserProfileCommand(
            user.Id.Value,
            "Jane Doe",
            "+1234567890",
            preferences
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        user.FullName.Value.Should().Be("Jane Doe");
        _mockUserRepository.Verify(r => r.SaveAsync(user), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullPreferences_ShouldSucceed()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var command = new UpdateUserProfileCommand(
            user.Id.Value,
            "Jane Doe",
            null,
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // Error Cases
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var command = new UpdateUserProfileCommand(
            userId,
            "Jane Doe",
            "+9876543210",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{userId}*");

        _mockUserRepository.Verify(r => r.SaveAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithSuspendedUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );
        user.Suspend("Account suspended");

        var command = new UpdateUserProfileCommand(
            user.Id.Value,
            "Jane Doe",
            "+9876543210",
            null
        );

        _mockUserRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var act = () => _handler.HandleAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*suspended*");
    }
}
