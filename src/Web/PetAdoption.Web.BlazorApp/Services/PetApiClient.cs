namespace PetAdoption.Web.BlazorApp.Services;

using System.Net.Http.Json;
using PetAdoption.Web.BlazorApp.Models;

public class PetApiClient
{
    private readonly HttpClient _http;

    public PetApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("PetApi");
    }

    public async Task<PetsResponse?> GetPetsAsync(string? status = null, Guid? petTypeId = null, int skip = 0, int take = 20)
    {
        var query = $"api/pets?skip={skip}&take={take}";
        if (status is not null) query += $"&status={status}";
        if (petTypeId.HasValue) query += $"&petTypeId={petTypeId}";
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

    public Task<HttpResponseMessage> AddFavoriteAsync(Guid petId) =>
        _http.PostAsJsonAsync("api/favorites", new { PetId = petId });

    public Task<HttpResponseMessage> RemoveFavoriteAsync(Guid petId) =>
        _http.DeleteAsync($"api/favorites/{petId}");

    public async Task<FavoritesResponse?> GetFavoritesAsync(int skip = 0, int take = 10) =>
        await _http.GetFromJsonAsync<FavoritesResponse>($"api/favorites?skip={skip}&take={take}");

    public async Task<IEnumerable<ActiveAnnouncement>?> GetActiveAnnouncementsAsync() =>
        await _http.GetFromJsonAsync<IEnumerable<ActiveAnnouncement>>("api/announcements/active");

    public async Task<AnnouncementsResponse?> GetAnnouncementsAsync(int skip = 0, int take = 10) =>
        await _http.GetFromJsonAsync<AnnouncementsResponse>($"api/announcements?skip={skip}&take={take}");

    public async Task<AnnouncementDetail?> GetAnnouncementAsync(Guid id) =>
        await _http.GetFromJsonAsync<AnnouncementDetail>($"api/announcements/{id}");

    public Task<HttpResponseMessage> CreateAnnouncementAsync(object request) =>
        _http.PostAsJsonAsync("api/announcements", request);

    public Task<HttpResponseMessage> UpdateAnnouncementAsync(Guid id, object request) =>
        _http.PutAsJsonAsync($"api/announcements/{id}", request);

    public Task<HttpResponseMessage> DeleteAnnouncementAsync(Guid id) =>
        _http.DeleteAsync($"api/announcements/{id}");
}
