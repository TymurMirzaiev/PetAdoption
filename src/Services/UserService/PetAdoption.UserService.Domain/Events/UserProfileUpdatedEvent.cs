namespace PetAdoption.UserService.Domain.Events;

public record UserProfileUpdatedEvent(
    string UserId,
    string? NewFullName,
    string? NewPhoneNumber,
    DateTime UpdatedAt
) : DomainEventBase;
