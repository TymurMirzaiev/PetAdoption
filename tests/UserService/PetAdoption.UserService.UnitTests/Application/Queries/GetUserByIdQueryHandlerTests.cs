namespace PetAdoption.UserService.Tests.Application.Queries;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Queries;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserQueryStore> _mockUserQueryStore;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _mockUserQueryStore = new Mock<IUserQueryStore>();
        _handler = new GetUserByIdQueryHandler(_mockUserQueryStore.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingUser_ShouldReturnUserDto()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            "+1234567890"
        );

        var query = new GetUserByIdQuery(user.Id.Value);

        _mockUserQueryStore
            .Setup(s => s.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id.Value);
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("John Doe");
        result.PhoneNumber.Should().Be("+1234567890");
        result.Status.Should().Be(UserStatus.Active.ToString());
        result.Role.Should().Be(UserRole.User.ToString());
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var query = new GetUserByIdQuery(userId);

        _mockUserQueryStore
            .Setup(s => s.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _handler.HandleAsync(query);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{userId}*");
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludePreferences()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var query = new GetUserByIdQuery(user.Id.Value);

        _mockUserQueryStore
            .Setup(s => s.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Preferences.Should().NotBeNull();
        result.Preferences.ReceiveEmailNotifications.Should().BeTrue();
        result.Preferences.ReceiveSmsNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeTimestamps()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var query = new GetUserByIdQuery(user.Id.Value);

        _mockUserQueryStore
            .Setup(s => s.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithNullPhoneNumber_ShouldReturnNull()
    {
        // Arrange
        var user = User.Register(
            "test@example.com",
            "John Doe",
            "$2a$12$hashedpassword",
            null
        );

        var query = new GetUserByIdQuery(user.Id.Value);

        _mockUserQueryStore
            .Setup(s => s.GetByIdAsync(It.IsAny<UserId>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.PhoneNumber.Should().BeNull();
    }
}
