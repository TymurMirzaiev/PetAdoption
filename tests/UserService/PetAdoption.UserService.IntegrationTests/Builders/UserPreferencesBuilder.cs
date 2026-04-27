using PetAdoption.UserService.Domain.ValueObjects;

namespace PetAdoption.UserService.IntegrationTests.Builders;

public class UserPreferencesBuilder
{
    private string? _preferredPetType;
    private List<string>? _preferredSizes;
    private string? _preferredAgeRange;
    private bool _receiveEmailNotifications = true;
    private bool _receiveSmsNotifications;

    public UserPreferencesBuilder WithPreferredPetType(string? petType) { _preferredPetType = petType; return this; }
    public UserPreferencesBuilder WithPreferredSizes(List<string>? sizes) { _preferredSizes = sizes; return this; }
    public UserPreferencesBuilder WithPreferredAgeRange(string? ageRange) { _preferredAgeRange = ageRange; return this; }
    public UserPreferencesBuilder WithReceiveEmailNotifications(bool value) { _receiveEmailNotifications = value; return this; }
    public UserPreferencesBuilder WithReceiveSmsNotifications(bool value) { _receiveSmsNotifications = value; return this; }

    public UserPreferences Build() => new()
    {
        PreferredPetType = _preferredPetType,
        PreferredSizes = _preferredSizes,
        PreferredAgeRange = _preferredAgeRange,
        ReceiveEmailNotifications = _receiveEmailNotifications,
        ReceiveSmsNotifications = _receiveSmsNotifications
    };

    public static UserPreferencesBuilder Default() => new UserPreferencesBuilder()
        .WithPreferredPetType("Dog")
        .WithPreferredSizes(new List<string> { "Medium", "Large" })
        .WithPreferredAgeRange("Adult")
        .WithReceiveEmailNotifications(true)
        .WithReceiveSmsNotifications(false);
}
