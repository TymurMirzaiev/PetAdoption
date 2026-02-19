namespace PetAdoption.UserService.Domain.Events;

public record UserRoleChangedEvent(
    string UserId,
    string NewRole,
    DateTime ChangedAt
) : DomainEventBase;
