namespace PetAdoption.Web.BlazorApp.Services;

using System.Net.Http.Json;
using PetAdoption.Web.BlazorApp.Models;

public class UserApiClient
{
    private readonly HttpClient _http;

    public UserApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("UserApi");
    }

    public Task<HttpResponseMessage> LoginAsync(LoginRequest request) =>
        _http.PostAsJsonAsync("api/users/login", request);

    public Task<HttpResponseMessage> RegisterAsync(RegisterRequest request) =>
        _http.PostAsJsonAsync("api/users/register", request);

    public Task<HttpResponseMessage> GoogleAuthAsync(GoogleAuthRequest request) =>
        _http.PostAsJsonAsync("api/users/auth/google", request);

    public Task<HttpResponseMessage> RefreshTokenAsync(RefreshTokenRequest request) =>
        _http.PostAsJsonAsync("api/users/refresh", request);

    public Task<HttpResponseMessage> LogoutAsync(string refreshToken) =>
        _http.PostAsJsonAsync("api/users/logout", new { RefreshToken = refreshToken });

    public async Task<UserProfile?> GetMyProfileAsync() =>
        await _http.GetFromJsonAsync<UserProfile>("api/users/me");

    public Task<HttpResponseMessage> UpdateProfileAsync(object request) =>
        _http.PutAsJsonAsync("api/users/me", request);

    public Task<HttpResponseMessage> ChangePasswordAsync(object request) =>
        _http.PostAsJsonAsync("api/users/me/change-password", request);

    public async Task<UsersResponse?> GetUsersAsync(int skip = 0, int take = 50) =>
        await _http.GetFromJsonAsync<UsersResponse>($"api/users?skip={skip}&take={take}");

    public Task<HttpResponseMessage> SuspendUserAsync(string userId, string reason) =>
        _http.PostAsJsonAsync($"api/users/{userId}/suspend", new { Reason = reason });

    public Task<HttpResponseMessage> ActivateUserAsync(string userId) =>
        _http.PostAsJsonAsync($"api/users/{userId}/activate", new { });

    public Task<HttpResponseMessage> PromoteToAdminAsync(string userId) =>
        _http.PostAsJsonAsync($"api/users/{userId}/promote-to-admin", new { });
}
