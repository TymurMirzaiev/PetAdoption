namespace PetAdoption.Web.BlazorApp.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using PetAdoption.Web.BlazorApp.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetAsync("accessToken");

        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        try
        {
            var jwt = _tokenHandler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await ClearTokens();
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = jwt.Claims.ToList();
            var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)
                ?? claims.FirstOrDefault(c => c.Type == "role");
            if (roleClaim is not null && !claims.Any(c => c.Type == ClaimTypes.Role))
                claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task Login(string accessToken, string refreshToken)
    {
        await _localStorage.SetAsync("accessToken", accessToken);
        await _localStorage.SetAsync("refreshToken", refreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task Logout()
    {
        await ClearTokens();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public Task<string?> GetAccessToken() => _localStorage.GetAsync("accessToken");

    public Task<string?> GetRefreshToken() => _localStorage.GetAsync("refreshToken");

    public async Task UpdateTokens(string accessToken, string refreshToken)
    {
        await _localStorage.SetAsync("accessToken", accessToken);
        await _localStorage.SetAsync("refreshToken", refreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task ClearTokens()
    {
        await _localStorage.RemoveAsync("accessToken");
        await _localStorage.RemoveAsync("refreshToken");
    }
}
