namespace PetAdoption.Web.BlazorApp.Services;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using PetAdoption.Web.BlazorApp.Models;

public class PetApiClient
{
    private readonly HttpClient _http;

    public PetApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("PetApi");
    }

    public async Task<PetsResponse?> GetPetsAsync(
        string? status = null, Guid? petTypeId = null, int skip = 0, int take = 20,
        int? minAge = null, int? maxAge = null, string? breed = null)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["skip"] = skip.ToString(),
            ["take"] = take.ToString(),
            ["status"] = status,
            ["petTypeId"] = petTypeId?.ToString(),
            ["minAge"] = minAge?.ToString(),
            ["maxAge"] = maxAge?.ToString(),
            ["breed"] = string.IsNullOrWhiteSpace(breed) ? null : breed
        };
        var query = QueryHelpers.AddQueryString("api/pets", parameters);
        return await _http.GetFromJsonAsync<PetsResponse>(query);
    }

    public async Task<PetDetails?> GetPetAsync(Guid id) =>
        await _http.GetFromJsonAsync<PetDetails>($"api/pets/{id}");

    public Task<HttpResponseMessage> CreatePetAsync(CreatePetRequest request) =>
        _http.PostAsJsonAsync("api/pets", request);

    public Task<HttpResponseMessage> UpdatePetAsync(Guid id, UpdatePetRequest request) =>
        _http.PutAsJsonAsync($"api/pets/{id}", request);

    public Task<HttpResponseMessage> DeletePetAsync(Guid id) =>
        _http.DeleteAsync($"api/pets/{id}");

    public Task<HttpResponseMessage> ReservePetAsync(Guid id) =>
        _http.PostAsync($"api/pets/{id}/reserve", null);

    public Task<HttpResponseMessage> AdoptPetAsync(Guid id) =>
        _http.PostAsync($"api/pets/{id}/adopt", null);

    public Task<HttpResponseMessage> CancelReservationAsync(Guid id) =>
        _http.PostAsync($"api/pets/{id}/cancel-reservation", null);

    public async Task<PetTypesResponse?> GetPetTypesAsync(bool includeInactive = false) =>
        await _http.GetFromJsonAsync<PetTypesResponse>($"api/admin/pet-types?includeInactive={includeInactive}");

    public Task<HttpResponseMessage> CreatePetTypeAsync(CreatePetTypeRequest request) =>
        _http.PostAsJsonAsync("api/admin/pet-types", request);

    public Task<HttpResponseMessage> UpdatePetTypeAsync(Guid id, UpdatePetTypeRequest request) =>
        _http.PutAsJsonAsync($"api/admin/pet-types/{id}", request);

    public Task<HttpResponseMessage> ActivatePetTypeAsync(Guid id) =>
        _http.PostAsync($"api/admin/pet-types/{id}/activate", null);

    public Task<HttpResponseMessage> DeactivatePetTypeAsync(Guid id) =>
        _http.PostAsync($"api/admin/pet-types/{id}/deactivate", null);

    public async Task<DiscoverPetsResponse?> GetDiscoverPetsAsync(
        Guid? petTypeId = null, int? minAge = null, int? maxAge = null,
        int take = 10, string? breed = null,
        decimal? lat = null, decimal? lng = null, int? radiusKm = null)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["take"] = take.ToString(),
            ["petTypeId"] = petTypeId?.ToString(),
            ["minAge"] = minAge?.ToString(),
            ["maxAge"] = maxAge?.ToString(),
            ["breed"] = string.IsNullOrWhiteSpace(breed) ? null : breed,
            ["lat"] = lat?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["lng"] = lng?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["radiusKm"] = radiusKm?.ToString()
        };
        var query = QueryHelpers.AddQueryString("api/discover", parameters);
        return await _http.GetFromJsonAsync<DiscoverPetsResponse>(query);
    }

    public Task<HttpResponseMessage> TrackSkipAsync(Guid petId) =>
        _http.PostAsJsonAsync("api/skips", new { PetId = petId });

    public Task<HttpResponseMessage> ResetSkipsAsync() =>
        _http.DeleteAsync("api/skips");

    public Task<HttpResponseMessage> AddFavoriteAsync(Guid petId) =>
        _http.PostAsJsonAsync("api/favorites", new { PetId = petId });

    public Task<HttpResponseMessage> RemoveFavoriteAsync(Guid petId) =>
        _http.DeleteAsync($"api/favorites/{petId}");

    public async Task<FavoritesResponse?> GetFavoritesAsync(
        int skip = 0, int take = 10,
        Guid? petTypeId = null, string? status = null, string sortBy = "newest")
    {
        var parameters = new Dictionary<string, string?>
        {
            ["skip"] = skip.ToString(),
            ["take"] = take.ToString(),
            ["sortBy"] = sortBy,
            ["petTypeId"] = petTypeId?.ToString(),
            ["status"] = status
        };
        var query = QueryHelpers.AddQueryString("api/favorites", parameters);
        return await _http.GetFromJsonAsync<FavoritesResponse>(query);
    }

    public async Task<bool> CheckFavoriteAsync(Guid petId)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<CheckFavoriteResponse>($"api/favorites/check/{petId}");
            return result?.IsFavorited ?? false;
        }
        catch { return false; }
    }

    public async Task<IEnumerable<ActiveAnnouncement>?> GetActiveAnnouncementsAsync() =>
        await _http.GetFromJsonAsync<IEnumerable<ActiveAnnouncement>>("api/announcements/active");

    public async Task<AnnouncementsResponse?> GetAnnouncementsAsync(int skip = 0, int take = 10) =>
        await _http.GetFromJsonAsync<AnnouncementsResponse>($"api/announcements?skip={skip}&take={take}");

    public async Task<AnnouncementDetail?> GetAnnouncementAsync(Guid id) =>
        await _http.GetFromJsonAsync<AnnouncementDetail>($"api/announcements/{id}");

    public Task<HttpResponseMessage> CreateAnnouncementAsync(CreateAnnouncementRequest request) =>
        _http.PostAsJsonAsync("api/announcements", request);

    public Task<HttpResponseMessage> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementRequest request) =>
        _http.PutAsJsonAsync($"api/announcements/{id}", request);

    public Task<HttpResponseMessage> DeleteAnnouncementAsync(Guid id) =>
        _http.DeleteAsync($"api/announcements/{id}");

    // Organization-scoped pet management
    public async Task<OrgPetsResponse?> GetOrgPetsAsync(Guid orgId, string? status = null, string? tags = null, int skip = 0, int take = 20)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["skip"] = skip.ToString(),
            ["take"] = take.ToString(),
            ["status"] = status,
            ["tags"] = tags
        };
        var query = QueryHelpers.AddQueryString($"api/organizations/{orgId}/pets", parameters);
        return await _http.GetFromJsonAsync<OrgPetsResponse>(query);
    }

    public Task<HttpResponseMessage> CreateOrgPetAsync(Guid orgId, CreateOrgPetRequest request) =>
        _http.PostAsJsonAsync($"api/organizations/{orgId}/pets", request);

    public Task<HttpResponseMessage> UpdateOrgPetAsync(Guid orgId, Guid petId, UpdateOrgPetRequest request) =>
        _http.PutAsJsonAsync($"api/organizations/{orgId}/pets/{petId}", request);

    public Task<HttpResponseMessage> DeleteOrgPetAsync(Guid orgId, Guid petId) =>
        _http.DeleteAsync($"api/organizations/{orgId}/pets/{petId}");

    // Adoption requests
    public Task<HttpResponseMessage> CreateAdoptionRequestAsync(Guid petId, string? message = null) =>
        _http.PostAsJsonAsync("api/adoption-requests", new { PetId = petId, Message = message });

    public async Task<AdoptionRequestsResponse?> GetMyAdoptionRequestsAsync(int skip = 0, int take = 20) =>
        await _http.GetFromJsonAsync<AdoptionRequestsResponse>($"api/adoption-requests/mine?skip={skip}&take={take}");

    public async Task<OrgAdoptionRequestsResponse?> GetOrgAdoptionRequestsAsync(
        Guid organizationId, string? status = null, int skip = 0, int take = 20)
    {
        var url = $"api/adoption-requests/organization/{organizationId}?skip={skip}&take={take}";
        if (status is not null) url += $"&status={status}";
        return await _http.GetFromJsonAsync<OrgAdoptionRequestsResponse>(url);
    }

    public Task<HttpResponseMessage> ApproveAdoptionRequestAsync(Guid requestId) =>
        _http.PostAsync($"api/adoption-requests/{requestId}/approve", null);

    public Task<HttpResponseMessage> RejectAdoptionRequestAsync(Guid requestId, string reason) =>
        _http.PostAsJsonAsync($"api/adoption-requests/{requestId}/reject", new { Reason = reason });

    public Task<HttpResponseMessage> CancelAdoptionRequestAsync(Guid requestId) =>
        _http.PostAsync($"api/adoption-requests/{requestId}/cancel", null);

    // Pet interaction metrics
    public Task<HttpResponseMessage> TrackInteractionAsync(Guid petId, string type) =>
        _http.PostAsJsonAsync($"api/pets/{petId}/interactions", new { Type = type });

    public Task<HttpResponseMessage> TrackBatchImpressionsAsync(IEnumerable<Guid> petIds) =>
        _http.PostAsJsonAsync("api/pets/interactions/batch", new { PetIds = petIds });

    public async Task<OrgMetricsResponse?> GetOrgMetricsAsync(
        Guid orgId, DateTime? from = null, DateTime? to = null,
        string? sortBy = null, bool descending = true)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["descending"] = descending.ToString().ToLowerInvariant(),
            ["from"] = from?.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["to"] = to?.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["sortBy"] = sortBy
        };
        var query = QueryHelpers.AddQueryString($"api/organizations/{orgId}/metrics", parameters);
        return await _http.GetFromJsonAsync<OrgMetricsResponse>(query);
    }

    public async Task<PetMetricsDetailResponse?> GetPetMetricsAsync(Guid petId)
    {
        return await _http.GetFromJsonAsync<PetMetricsDetailResponse>($"api/pets/{petId}/metrics");
    }

    public async Task<OrgDashboardResponse?> GetOrgDashboardAsync(Guid orgId) =>
        await _http.GetFromJsonAsync<OrgDashboardResponse>($"api/organizations/{orgId}/dashboard");

    public async Task<HttpResponseMessage> GetOrgDashboardTrendsAsync(Guid orgId, DateTime? from, DateTime? to)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["from"] = from?.ToString("O"),
            ["to"] = to?.ToString("O")
        };
        var query = QueryHelpers.AddQueryString($"api/organizations/{orgId}/dashboard/trends", parameters);
        return await _http.GetAsync(query);
    }

    // Pet media
    public async Task<IReadOnlyList<PetMediaItem>> GetPetMediaAsync(Guid petId) =>
        await _http.GetFromJsonAsync<IReadOnlyList<PetMediaItem>>($"api/pets/{petId}/media") ?? [];

    public Task<HttpResponseMessage> UploadPetMediaAsync(Guid orgId, Guid petId, IBrowserFile file)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024)), "file", file.Name);
        return _http.PostAsync($"api/organizations/{orgId}/pets/{petId}/media", content);
    }

    public Task<HttpResponseMessage> DeletePetMediaAsync(Guid orgId, Guid petId, Guid mediaId) =>
        _http.DeleteAsync($"api/organizations/{orgId}/pets/{petId}/media/{mediaId}");

    public Task<HttpResponseMessage> ReorderPetPhotosAsync(Guid orgId, Guid petId, IReadOnlyList<Guid> orderedIds) =>
        _http.PutAsJsonAsync($"api/organizations/{orgId}/pets/{petId}/media/order", new { OrderedIds = orderedIds });

    public Task<HttpResponseMessage> SetPrimaryPhotoAsync(Guid orgId, Guid petId, Guid mediaId) =>
        _http.PutAsync($"api/organizations/{orgId}/pets/{petId}/media/{mediaId}/primary", null);

    // Pet medical record
    public Task<HttpResponseMessage> UpdatePetMedicalRecordAsync(Guid orgId, Guid petId, UpdatePetMedicalRecordRequest request) =>
        _http.PutAsJsonAsync($"api/organizations/{orgId}/pets/{petId}/medical-record", request);

    // Chat
    public async Task<IReadOnlyList<ChatMessageDto>> GetChatHistoryAsync(
        Guid requestId, Guid? afterId = null, int take = 50)
    {
        var url = $"api/adoption-requests/{requestId}/messages?take={take}";
        if (afterId.HasValue) url += $"&afterId={afterId}";
        return await _http.GetFromJsonAsync<IReadOnlyList<ChatMessageDto>>(url) ?? [];
    }

    public Task<HttpResponseMessage> SendChatMessageAsync(Guid requestId, string body) =>
        _http.PostAsJsonAsync($"api/adoption-requests/{requestId}/messages", new { Body = body });

    public async Task<int> MarkChatThreadReadAsync(Guid requestId)
    {
        var r = await _http.PostAsync($"api/adoption-requests/{requestId}/messages/mark-read", null);
        if (r.IsSuccessStatusCode)
        {
            var d = await r.Content.ReadFromJsonAsync<MarkReadResponse>();
            return d?.Marked ?? 0;
        }
        return 0;
    }

    public async Task<int> GetMyChatUnreadTotalAsync()
    {
        try
        {
            var d = await _http.GetFromJsonAsync<UnreadTotalResponse>("api/me/chat/unread-total");
            return d?.TotalUnread ?? 0;
        }
        catch { return 0; }
    }

    public async Task<int> GetOrgChatUnreadTotalAsync(Guid orgId)
    {
        try
        {
            var d = await _http.GetFromJsonAsync<UnreadTotalResponse>($"api/organizations/{orgId}/chat/unread-total");
            return d?.TotalUnread ?? 0;
        }
        catch { return 0; }
    }

    private record MarkReadResponse(int Marked);
    private record UnreadTotalResponse(int TotalUnread);
}
