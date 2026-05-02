namespace PetAdoption.UserService.Infrastructure.Security;

using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PetAdoption.UserService.Application.Abstractions;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;

    public GoogleTokenValidator(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured");
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<GoogleTokenInfo>(
                $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}");

            if (response is null || response.Aud != _clientId)
                return null;

            if (string.IsNullOrEmpty(response.Email) || response.EmailVerified != "true")
                return null;

            return new GoogleUserInfo(response.Email, response.Name ?? response.Email);
        }
        catch (HttpRequestException)
        {
            throw; // infrastructure failure — let it propagate so the caller returns 503
        }
        catch
        {
            // Token validation failure (expired, malformed, wrong audience, etc.)
            return null;
        }
    }

    private record GoogleTokenInfo(string? Email, string? EmailVerified, string? Name, string? Aud);
}
