namespace PetAdoption.Web.BlazorApp.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthorizationMessageHandler(IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory)
    {
        _jsRuntime = jsRuntime;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "accessToken");

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
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var httpClient = _httpClientFactory.CreateClient("UserApiDirect");
            var response = await httpClient.PostAsJsonAsync(
                "api/users/refresh",
                new { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (result is null) return null;

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", result.AccessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", result.RefreshToken);
            return result.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private record TokenResponse(string AccessToken, string RefreshToken);
}
