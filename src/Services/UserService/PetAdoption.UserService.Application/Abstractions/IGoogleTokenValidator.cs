namespace PetAdoption.UserService.Application.Abstractions;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken);
}

public record GoogleUserInfo(string Email, string FullName);
