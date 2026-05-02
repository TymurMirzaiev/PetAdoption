using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PetAdoption.PetService.IntegrationTests.Builders;
using PetAdoption.PetService.IntegrationTests.Infrastructure;
using Xunit;

namespace PetAdoption.PetService.IntegrationTests.Tests;

[Collection("SqlServer")]
public class PetMediaControllerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sqlFixture;
    private PetServiceWebAppFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly Guid TestOrgId = Guid.NewGuid();

    public PetMediaControllerTests(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new PetServiceWebAppFactory(_sqlFixture.ConnectionString);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "test-org-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", TestOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Upload
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadPhoto_WithValidJpeg_ReturnsOk()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var content = BuildImageContent("image/jpeg", "photo.jpg");

        // Act
        var response = await _client.PostAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UploadMediaResponseDto>();
        result!.MediaType.Should().Be("Photo");
    }

    [Fact]
    public async Task UploadMedia_WithInvalidContentType_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var content = BuildImageContent("text/plain", "file.txt");

        // Act
        var response = await _client.PostAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media", content);

        // Assert
        // invalid content type → DomainException(InvalidOperation) → 409
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UploadMedia_FromWrongOrg_ReturnsForbidden()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);

        var wrongOrgClient = _factory.CreateClient();
        var wrongOrgId = Guid.NewGuid();
        wrongOrgClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken(
                userId: "other-user",
                role: "User",
                additionalClaims: new Dictionary<string, string>
                {
                    { "organizationId", wrongOrgId.ToString() },
                    { "orgRole", "Admin" }
                }));
        var content = BuildImageContent("image/jpeg", "photo.jpg");

        // Act — route orgId matches TestOrgId but caller claims wrongOrgId
        var response = await wrongOrgClient.PostAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ──────────────────────────────────────────────────────────────
    // Get media list
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMedia_AfterUpload_ReturnsItem()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var uploadContent = BuildImageContent("image/jpeg", "photo.jpg");
        await _client.PostAsync($"/api/organizations/{TestOrgId}/pets/{petId}/media", uploadContent);

        // Act — publicly accessible endpoint
        var publicClient = _factory.CreateClient();
        var response = await publicClient.GetAsync($"/api/pets/{petId}/media");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetMediaResponseDto>();
        result!.Items.Should().HaveCount(1);
        result.Items[0].IsPrimary.Should().BeTrue();
        result.Items[0].MediaType.Should().Be("Photo");
    }

    [Fact]
    public async Task GetMedia_WithNoMedia_ReturnsEmptyList()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);

        // Act
        var publicClient = _factory.CreateClient();
        var response = await publicClient.GetAsync($"/api/pets/{petId}/media");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPetMediaResponseDto>();
        result!.Items.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // Delete
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMedia_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var uploadContent = BuildImageContent("image/jpeg", "photo.jpg");
        var uploadResponse = await _client.PostAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media", uploadContent);
        var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<UploadMediaResponseDto>();
        var mediaId = uploadResult!.Id;

        // Act
        var deleteResponse = await _client.DeleteAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media/{mediaId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removed
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetAsync($"/api/pets/{petId}/media");
        var getResult = await getResponse.Content.ReadFromJsonAsync<GetPetMediaResponseDto>();
        getResult!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMedia_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        var nonExistentMediaId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media/{nonExistentMediaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────
    // Reorder
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderPhotos_WithValidOrder_ReturnsNoContent()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);

        var firstId = await UploadPhotoAsync(petId, "photo1.jpg");
        var secondId = await UploadPhotoAsync(petId, "photo2.jpg");

        // Act — swap order
        var reorderRequest = new { OrderedIds = new[] { secondId, firstId } };
        var response = await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media/order", reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReorderPhotos_WithMismatchedIds_ReturnsConflict()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        await UploadPhotoAsync(petId, "photo.jpg");

        // Act — wrong ids
        var reorderRequest = new { OrderedIds = new[] { Guid.NewGuid() } };
        var response = await _client.PutAsJsonAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media/order", reorderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ──────────────────────────────────────────────────────────────
    // Set Primary
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetPrimary_SecondPhoto_ReturnsNoContent()
    {
        // Arrange
        var petTypeId = await SeedPetTypeAsync();
        var petId = await CreateOrgPetAsync(petTypeId);
        await UploadPhotoAsync(petId, "photo1.jpg");
        var secondId = await UploadPhotoAsync(petId, "photo2.jpg");

        // Act
        var response = await _client.PutAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media/{secondId}/primary", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetAsync($"/api/pets/{petId}/media");
        var getResult = await getResponse.Content.ReadFromJsonAsync<GetPetMediaResponseDto>();
        getResult!.Items.First(m => m.Id == secondId).IsPrimary.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<Guid> SeedPetTypeAsync()
    {
        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", PetServiceWebAppFactory.GenerateTestToken());

        var response = await adminClient.PostAsJsonAsync("/api/admin/pet-types", new { Code = "dog", Name = "Dog" });
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var result = await response.Content.ReadFromJsonAsync<CreatePetTypeResponseDto>();
            return result!.Id;
        }

        var allTypesResponse = await adminClient.GetAsync("/api/admin/pet-types?includeInactive=true");
        var allTypes = await allTypesResponse.Content.ReadFromJsonAsync<List<PetTypeResponseDto>>();
        return allTypes!.First(t => t.Code == "dog").Id;
    }

    private async Task<Guid> CreateOrgPetAsync(Guid petTypeId)
    {
        var request = CreateOrgPetRequestBuilder.Default()
            .WithName("MediaPet")
            .WithPetTypeId(petTypeId)
            .Build();

        var response = await _client.PostAsJsonAsync($"/api/organizations/{TestOrgId}/pets", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateOrgPetResponseDto>();
        return result!.Id;
    }

    private async Task<Guid> UploadPhotoAsync(Guid petId, string fileName)
    {
        var content = BuildImageContent("image/jpeg", fileName);
        var response = await _client.PostAsync(
            $"/api/organizations/{TestOrgId}/pets/{petId}/media", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<UploadMediaResponseDto>();
        return result!.Id;
    }

    private static MultipartFormDataContent BuildImageContent(string contentType, string fileName)
    {
        // Minimal valid 1×1 pixel JPEG bytes
        var imageBytes = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        };

        var formContent = new MultipartFormDataContent();
        var streamContent = new StreamContent(new MemoryStream(imageBytes));
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        formContent.Add(streamContent, "file", fileName);
        return formContent;
    }

    // ──────────────────────────────────────────────────────────────
    // Response DTOs
    // ──────────────────────────────────────────────────────────────

    private record UploadMediaResponseDto(Guid Id, string Url, string MediaType);
    private record GetPetMediaResponseDto(List<PetMediaItemDto> Items);
    private record PetMediaItemDto(Guid Id, string MediaType, string Url, string ContentType, int SortOrder, bool IsPrimary, DateTime CreatedAt);
    private record CreatePetTypeResponseDto(Guid Id);
    private record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
    private record CreateOrgPetResponseDto(Guid Id, string Name);
}
