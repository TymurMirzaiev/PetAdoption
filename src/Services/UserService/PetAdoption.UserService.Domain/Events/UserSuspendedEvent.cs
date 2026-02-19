namespace PetAdoption.UserService.Domain.Events;

public record UserSuspendedEvent(
    string UserId,
    string Reason,
    DateTime SuspendedAt
) : DomainEventBase;
