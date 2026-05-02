using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class ChatControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;

    private static readonly Guid TestOrgId = Guid.NewGuid();
    private static readonly string ShelterUserId = Guid.NewGuid().ToString();
    private static readonly string AdopterUserId = Guid.NewGuid().ToString();

    private HttpClient _shelterClient = null!;
    private HttpClient _adopterClient = null!;

    public ChatControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);

        _shelterClient = _factory.CreateClient();
        _shelterClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: ShelterUserId,
                role: "Admin",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));

        _adopterClient = _factory.CreateClient();
        _adopterClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: AdopterUserId,
                role: "User"));

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _shelterClient.Dispose();
        _adopterClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync(string code = "chat_dog")
    {
        var request = new CreatePetTypeRequestBuilder().WithCode(code).WithName("Chat Dog").Build();
        var response = await _shelterClient.PostAsJsonAsync("/api/admin/pet-types", request);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<PetTypeResponseDto>();
            return result!.Id;
        }
        var allTypes = await _shelterClient.GetFromJsonAsync<List<PetTypeResponseDto>>("/api/admin/pet-types?includeInactive=true");
        return allTypes!.First(t => t.Code == code).Id;
    }

    private async Task<Guid> SeedPetWithOrgAsync(string name, Guid petTypeId)
    {
        await using var db = _factory.CreateDbContext();
        var pet = Pet.Create(name, petTypeId, breed: null, ageMonths: 12, description: null);
        pet.AssignToOrganization(TestOrgId);
        db.Pets.Add(pet);
        await db.SaveChangesAsync();
        return pet.Id;
    }

    private async Task<Guid> CreateAdoptionRequestAsync(Guid petId)
    {
        var response = await _adopterClient.PostAsJsonAsync("/api/adoption-requests", new { PetId = petId, Message = "Hi" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AdoptionRequestResultDto>();
        return result!.Id;
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/adoption-requests/{id}/messages
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_WhenShelterSendsAndAdopterReads_ReturnsMessages()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_rt");
        var petId = await SeedPetWithOrgAsync("RoundTripDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Hello from shelter" });

        // Act
        var response = await _adopterClient.GetAsync($"/api/adoption-requests/{requestId}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GetChatHistoryResponseDto>();
        dto.Should().NotBeNull();
        dto!.Messages.Should().HaveCount(1);
        dto.Messages[0].Body.Should().Be("Hello from shelter");
        dto.Messages[0].SenderRole.Should().Be("Shelter");
    }

    [Fact]
    public async Task GetHistory_CrossUserAccess_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_cross");
        var petId = await SeedPetWithOrgAsync("CrossUserDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        using var strangerClient = _factory.CreateClient();
        strangerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", PetServiceWebAppFactory.GenerateTestToken(userId: Guid.NewGuid().ToString(), role: "User"));

        // Act
        var response = await strangerClient.GetAsync($"/api/adoption-requests/{requestId}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHistory_AnonymousRequest_ReturnsUnauthorized()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_anon");
        var petId = await SeedPetWithOrgAsync("AnonDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        using var anonClient = _factory.CreateClient();

        // Act
        var response = await anonClient.GetAsync($"/api/adoption-requests/{requestId}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests/{id}/messages
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_OnRejectedRequest_ReturnsConflict_ChatThreadClosed()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_rej");
        var petId = await SeedPetWithOrgAsync("RejectedDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        // Reject the request
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/reject", new { Reason = "Not a fit" });

        // Act
        var response = await _adopterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Can we reconsider?" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error!.ErrorCode.Should().Be("chat_thread_closed");
    }

    // ──────────────────────────────────────────────────────────────
    // Pagination via afterId
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_WithAfterId_ReturnsPaginatedResults()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_page");
        var petId = await SeedPetWithOrgAsync("PageDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        // Send 3 messages
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Message 1" });
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Message 2" });
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Message 3" });

        // Get all messages first
        var allResponse = await _adopterClient.GetFromJsonAsync<GetChatHistoryResponseDto>(
            $"/api/adoption-requests/{requestId}/messages?take=2");
        allResponse!.Messages.Should().HaveCount(2);
        var firstMessageId = allResponse.Messages[0].Id;

        // Act — get messages after the first one
        var pagedResponse = await _adopterClient.GetFromJsonAsync<GetChatHistoryResponseDto>(
            $"/api/adoption-requests/{requestId}/messages?afterId={firstMessageId}&take=10");

        // Assert
        pagedResponse.Should().NotBeNull();
        pagedResponse!.Messages.Should().HaveCount(2);
        pagedResponse.Messages.Should().NotContain(m => m.Id == firstMessageId);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/adoption-requests/{id}/messages/mark-read
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_SetsReadByRecipientAt()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_read");
        var petId = await SeedPetWithOrgAsync("ReadDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        // Shelter sends a message
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Have you read this?" });

        // Act — adopter marks as read
        var markResponse = await _adopterClient.PostAsync(
            $"/api/adoption-requests/{requestId}/messages/mark-read", null);

        // Assert
        markResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await markResponse.Content.ReadFromJsonAsync<MarkReadResponseDto>();
        result!.Marked.Should().Be(1);

        // Verify: re-fetching history shows the message is read
        var historyResponse = await _adopterClient.GetFromJsonAsync<GetChatHistoryResponseDto>(
            $"/api/adoption-requests/{requestId}/messages");
        historyResponse!.Messages.Should().HaveCount(1);
        historyResponse.Messages[0].ReadByRecipientAt.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // Closed thread is still readable
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_OnClosedThread_ReturnsOk()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync("chat_dog_closed");
        var petId = await SeedPetWithOrgAsync("ClosedDog", petTypeId);
        var requestId = await CreateAdoptionRequestAsync(petId);

        // Send a message before closing
        await _adopterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/messages", new { Body = "Interested!" });

        // Reject the request (closes the thread)
        await _shelterClient.PostAsJsonAsync(
            $"/api/adoption-requests/{requestId}/reject", new { Reason = "Not suitable" });

        // Act
        var response = await _adopterClient.GetAsync($"/api/adoption-requests/{requestId}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GetChatHistoryResponseDto>();
        dto!.Messages.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────
    // Private Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record AdoptionRequestResultDto(Guid Id, string Status);

    private record ChatMessageResponseDto(
        Guid Id, Guid AdoptionRequestId, Guid SenderUserId, string SenderRole,
        string Body, DateTime SentAt, DateTime? ReadByRecipientAt);

    private record GetChatHistoryResponseDto(List<ChatMessageResponseDto> Messages, bool HasMore);

    private record MarkReadResponseDto(int Marked);

    private record ErrorDto(string ErrorCode, string Message);

    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
}
