namespace PetAdoption.Web.BlazorApp.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtAuthenticationStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "accessToken");

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
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", accessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task Logout()
    {
        await ClearTokens();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<string?> GetAccessToken() =>
        await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "accessToken");

    public async Task<string?> GetRefreshToken() =>
        await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "refreshToken");

    public async Task UpdateTokens(string accessToken, string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "accessToken", accessToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "refreshToken", refreshToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task ClearTokens()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "accessToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
    }
}
