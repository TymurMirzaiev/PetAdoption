namespace PetAdoption.UserService.Domain.Events;

public record UserProfileUpdatedEvent(
    string UserId,
    string? NewFullName,
    string? NewPhoneNumber,
    string? NewBio,
    DateTime UpdatedAt
) : DomainEventBase;
