namespace PetAdoption.UserService.Domain.Events;

public record UserPasswordChangedEvent(
    string UserId,
    DateTime ChangedAt
) : DomainEventBase;
