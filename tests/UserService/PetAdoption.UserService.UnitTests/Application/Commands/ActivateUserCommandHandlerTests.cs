namespace PetAdoption.UserService.UnitTests.Application.Commands;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.Enums;
using PetAdoption.UserService.Domain.ValueObjects;

public class ActivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly ActivateUserCommandHandler _handler;

    public ActivateUserCommandHandlerTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _handler = new ActivateUserCommandHandler(_mockUserRepo.Object);
    }

    // ──────────────────────────────────────────────────────────────
    // Success
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SuspendedUser_ShouldActivate()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test User", "$2a$12$hash");
        user.Suspend("test reason");
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(new ActivateUserCommand(user.Id.Value));

        // Assert
        result.Success.Should().BeTrue();
        _mockUserRepo.Verify(r => r.SaveAsync(It.Is<User>(u => u.Status == UserStatus.Active)), Times.Once);
    }

    // ──────────────────────────────────────────────────────────────
    // Errors
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NonExistentUser_ShouldThrow()
    {
        // Arrange
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync((User?)null);

        // Act & Assert
        var act = () => _handler.HandleAsync(new ActivateUserCommand("non-existent"));
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task HandleAsync_AlreadyActiveUser_ShouldThrow()
    {
        // Arrange
        var user = User.Register("test@example.com", "Test User", "$2a$12$hash");
        _mockUserRepo.Setup(r => r.GetByIdAsync(It.IsAny<UserId>())).ReturnsAsync(user);

        // Act & Assert
        var act = () => _handler.HandleAsync(new ActivateUserCommand(user.Id.Value));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
