namespace PetAdoption.Web.BlazorApp.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PetAdoption.Web.BlazorApp.Services;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthorizationMessageHandler(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory)
    {
        _localStorage = localStorage;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetAsync("accessToken");

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var jwt = _tokenHandler.ReadJwtToken(token);
                if (jwt.ValidTo < DateTime.UtcNow.AddMinutes(1))
                    token = await TryRefreshToken();
            }
            catch
            {
                token = await TryRefreshToken();
            }

            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> TryRefreshToken()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var refreshToken = await _localStorage.GetAsync("refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var httpClient = _httpClientFactory.CreateClient("UserApiDirect");
            var response = await httpClient.PostAsJsonAsync(
                "api/users/refresh",
                new { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                await _localStorage.RemoveAsync("accessToken");
                await _localStorage.RemoveAsync("refreshToken");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result is null) return null;

            await _localStorage.SetAsync("accessToken", result.AccessToken);
            await _localStorage.SetAsync("refreshToken", result.RefreshToken);
            return result.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private record TokenResponse(string AccessToken, string RefreshToken);
}
