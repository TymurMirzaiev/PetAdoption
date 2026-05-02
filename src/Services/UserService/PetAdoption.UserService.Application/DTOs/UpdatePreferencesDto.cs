namespace PetAdoption.UserService.Application.DTOs;

public record UpdatePreferencesDto(
    string? PreferredPetType,
    List<string>? PreferredSizes,
    string? PreferredAgeRange,
    bool ReceiveEmailNotifications,
    bool ReceiveSmsNotifications
);
