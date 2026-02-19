namespace PetAdoption.UserService.Domain.ValueObjects;

public record UserPreferences
{
    public string? PreferredPetType { get; init; } // Dog, Cat, etc.
    public List<string>? PreferredSizes { get; init; } // Small, Medium, Large
    public string? PreferredAgeRange { get; init; } // Young, Adult, Senior
    public bool ReceiveEmailNotifications { get; init; } = true;
    public bool ReceiveSmsNotifications { get; init; } = false;

    public static UserPreferences Default() => new()
    {
        ReceiveEmailNotifications = true,
        ReceiveSmsNotifications = false
    };
}
