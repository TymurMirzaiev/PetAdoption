using PetAdoption.UserService.Domain.ValueObjects;

namespace PetAdoption.UserService.IntegrationTests.Builders;

public class UpdateProfileRequestBuilder
{
    private string? _fullName;
    private string? _phoneNumber;
    private UserPreferences? _preferences;
    private string? _bio;

    public UpdateProfileRequestBuilder WithFullName(string? fullName) { _fullName = fullName; return this; }
    public UpdateProfileRequestBuilder WithPhoneNumber(string? phoneNumber) { _phoneNumber = phoneNumber; return this; }
    public UpdateProfileRequestBuilder WithPreferences(UserPreferences? preferences) { _preferences = preferences; return this; }
    public UpdateProfileRequestBuilder WithBio(string? bio) { _bio = bio; return this; }

    public object Build() => new
    {
        FullName = _fullName,
        PhoneNumber = _phoneNumber,
        Preferences = _preferences,
        Bio = _bio
    };

    public static UpdateProfileRequestBuilder Default() => new UpdateProfileRequestBuilder()
        .WithFullName("Updated Name");
}
