namespace PetAdoption.UserService.Domain.Events;

public record UserRegisteredEvent(
    string UserId,
    string Email,
    string FullName,
    string Role,
    DateTime RegisteredAt
) : DomainEventBase;
