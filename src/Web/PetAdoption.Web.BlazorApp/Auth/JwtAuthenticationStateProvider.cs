namespace PetAdoption.Web.BlazorApp.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
