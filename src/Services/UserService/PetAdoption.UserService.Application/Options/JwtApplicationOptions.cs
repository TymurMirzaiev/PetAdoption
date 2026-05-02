namespace PetAdoption.UserService.Application.Options;

public class JwtApplicationOptions
{
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
