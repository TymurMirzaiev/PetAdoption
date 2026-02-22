namespace PetAdoption.UserService.Tests.Application.Queries;

using FluentAssertions;
using Moq;
using PetAdoption.UserService.Application.Queries;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserQueryStore> _mockUserQueryStore;
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _mockUserQueryStore = new Mock<IUserQueryStore>();
        _handler = new GetUsersQueryHandler(_mockUserQueryStore.Object);
    }

    [Fact]
    public async Task HandleAsync_WithUsers_ShouldReturnUserList()
    {
        // Arrange
        var user1 = User.Register("user1@example.com", "User One", "$2a$12$hash1", null);
        var user2 = User.Register("user2@example.com", "User Two", "$2a$12$hash2", null);
        var users = new List<User> { user1, user2 };

        var query = new GetUsersQuery(0, 50);

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(0, 50))
            .ReturnsAsync(users);

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(2);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Users.Should().HaveCount(2);
        result.Total.Should().Be(2);
        result.Skip.Should().Be(0);
        result.Take.Should().Be(50);
    }

    [Fact]
    public async Task HandleAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var user1 = User.Register("user1@example.com", "User One", "$2a$12$hash1", null);
        var users = new List<User> { user1 };

        var query = new GetUsersQuery(10, 20);

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(10, 20))
            .ReturnsAsync(users);

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(100);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Skip.Should().Be(10);
        result.Take.Should().Be(20);
        result.Total.Should().Be(100);
        result.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetUsersQuery(0, 50);

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(0, 50))
            .ReturnsAsync(new List<User>());

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Users.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapToListItemDto()
    {
        // Arrange
        var user = User.Register("test@example.com", "John Doe", "$2a$12$hash", "+1234567890");
        var users = new List<User> { user };

        var query = new GetUsersQuery(0, 50);

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(0, 50))
            .ReturnsAsync(users);

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        var userDto = result.Users.First();
        userDto.Id.Should().Be(user.Id.Value);
        userDto.Email.Should().Be("test@example.com");
        userDto.FullName.Should().Be("John Doe");
        userDto.Status.Should().Be("Active");
        userDto.Role.Should().Be("User");
        userDto.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task HandleAsync_ShouldCallGetAllWithCorrectParams()
    {
        // Arrange
        var query = new GetUsersQuery(5, 15);

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(5, 15))
            .ReturnsAsync(new List<User>());

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(0);

        // Act
        await _handler.HandleAsync(query);

        // Assert
        _mockUserQueryStore.Verify(s => s.GetAllAsync(5, 15), Times.Once);
        _mockUserQueryStore.Verify(s => s.CountAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var query = new GetUsersQuery(); // Uses defaults: Skip=0, Take=50

        _mockUserQueryStore
            .Setup(s => s.GetAllAsync(0, 50))
            .ReturnsAsync(new List<User>());

        _mockUserQueryStore
            .Setup(s => s.CountAsync())
            .ReturnsAsync(0);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Skip.Should().Be(0);
        result.Take.Should().Be(50);
        _mockUserQueryStore.Verify(s => s.GetAllAsync(0, 50), Times.Once);
    }
}
