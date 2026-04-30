using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class AnnouncementsControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();

    public AnnouncementsControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: TestUserId));
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateAnnouncementAsync(
        string title = "Test Announcement",
        string body = "Test body",
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-1);
        var end = endDate ?? DateTime.UtcNow.AddDays(7);
        var response = await _client.PostAsJsonAsync("/api/announcements", new
        {
            Title = title,
            Body = body,
            StartDate = start,
            EndDate = end
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateAnnouncementResponseDto>();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // Create
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAnnouncement_WithValidData_ReturnsCreated()
    {
        // Arrange & Act
        var response = await _client.PostAsJsonAsync("/api/announcements", new
        {
            Title = "Holiday Hours",
            Body = "We will be closed on Monday",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateAnnouncementResponseDto>();
        result!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAnnouncement_WithEndBeforeStart_ReturnsBadRequest()
    {
        // Arrange & Act
        var response = await _client.PostAsJsonAsync("/api/announcements", new
        {
            Title = "Bad Dates",
            Body = "Body",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAnnouncement_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: TestUserId, role: "User"));

        // Act
        var response = await client.PostAsJsonAsync("/api/announcements", new
        {
            Title = "Title",
            Body = "Body",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAnnouncement_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/announcements", new
        {
            Title = "Title",
            Body = "Body",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // Update
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAnnouncement_Existing_ReturnsOk()
    {
        // Arrange
        var id = await CreateAnnouncementAsync();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/announcements/{id}", new
        {
            Title = "Updated Title",
            Body = "Updated body",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(14)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateAnnouncementResponseDto>();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task UpdateAnnouncement_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.PutAsJsonAsync($"/api/announcements/{Guid.NewGuid()}", new
        {
            Title = "Title",
            Body = "Body",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAnnouncement_VerifyChanges_ReturnsUpdatedData()
    {
        // Arrange
        var id = await CreateAnnouncementAsync();
        var newStart = DateTime.UtcNow.AddDays(1);
        var newEnd = DateTime.UtcNow.AddDays(30);

        // Act
        await _client.PutAsJsonAsync($"/api/announcements/{id}", new
        {
            Title = "New Title",
            Body = "New Body",
            StartDate = newStart,
            EndDate = newEnd
        });

        var getResponse = await _client.GetAsync($"/api/announcements/{id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getResponse.Content.ReadFromJsonAsync<AnnouncementDetailResponseDto>();
        detail!.Title.Should().Be("New Title");
        detail.Body.Should().Be("New Body");
    }

    // ──────────────────────────────────────────────────────────────
    // Delete
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAnnouncement_Existing_ReturnsNoContent()
    {
        // Arrange
        var id = await CreateAnnouncementAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/announcements/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteAnnouncement_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/announcements/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAnnouncement_AlreadyDeleted_ReturnsNotFound()
    {
        // Arrange
        var id = await CreateAnnouncementAsync();
        await _client.DeleteAsync($"/api/announcements/{id}");

        // Act
        var response = await _client.DeleteAsync($"/api/announcements/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // Get By Id
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        // Arrange
        var id = await CreateAnnouncementAsync("Detail Test", "Detail body");

        // Act
        var response = await _client.GetAsync($"/api/announcements/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await response.Content.ReadFromJsonAsync<AnnouncementDetailResponseDto>();
        detail!.Id.Should().Be(id);
        detail.Title.Should().Be("Detail Test");
        detail.Body.Should().Be("Detail body");
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/announcements/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // Get All (Paginated)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithAnnouncements_ReturnsList()
    {
        // Arrange
        await CreateAnnouncementAsync("Announcement 1");
        await CreateAnnouncementAsync("Announcement 2");

        // Act
        var response = await _client.GetAsync("/api/announcements");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAnnouncementsResponseDto>();
        body!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        body.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAll_WithPagination_RespectsSkipAndTake()
    {
        // Arrange
        await CreateAnnouncementAsync("Page 1");
        await CreateAnnouncementAsync("Page 2");
        await CreateAnnouncementAsync("Page 3");

        // Act
        var response = await _client.GetAsync("/api/announcements?skip=0&take=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetAnnouncementsResponseDto>();
        body!.Items.Count().Should().BeLessThanOrEqualTo(2);
        body.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }

    // ──────────────────────────────────────────────────────────────
    // Get Active (Anonymous)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetActive_ReturnsOnlyCurrentAnnouncements()
    {
        // Arrange - create one active and one expired
        await CreateAnnouncementAsync("Active", startDate: DateTime.UtcNow.AddDays(-1), endDate: DateTime.UtcNow.AddDays(7));
        await CreateAnnouncementAsync("Expired", startDate: DateTime.UtcNow.AddDays(-10), endDate: DateTime.UtcNow.AddDays(-5));

        // Act
        var response = await _client.GetAsync("/api/announcements/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ActiveAnnouncementResponseDto>>();
        items.Should().NotBeNull();
        items!.Should().OnlyContain(a => a.Title != "Expired");
    }

    [Fact]
    public async Task GetActive_WithoutAuth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/announcements/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActive_ReturnsActiveAnnouncementFields()
    {
        // Arrange
        await CreateAnnouncementAsync("Field Check", "Field body content",
            startDate: DateTime.UtcNow.AddDays(-1), endDate: DateTime.UtcNow.AddDays(7));

        // Act
        var response = await _client.GetAsync("/api/announcements/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ActiveAnnouncementResponseDto>>();
        var match = items!.FirstOrDefault(a => a.Title == "Field Check");
        match.Should().NotBeNull();
        match!.Body.Should().Be("Field body content");
        match.Id.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record CreateAnnouncementResponseDto(Guid Id);
    private record UpdateAnnouncementResponseDto(Guid Id);
    private record AnnouncementDetailResponseDto(Guid Id, string Title, string Body, DateTime StartDate, DateTime EndDate, Guid CreatedBy, DateTime CreatedAt);
    private record GetAnnouncementsResponseDto(IEnumerable<AnnouncementListItemDto> Items, long TotalCount, int Skip, int Take);
    private record AnnouncementListItemDto(Guid Id, string Title, DateTime StartDate, DateTime EndDate, string Status, DateTime CreatedAt);
    private record ActiveAnnouncementResponseDto(Guid Id, string Title, string Body);
}
