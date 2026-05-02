namespace PetAdoption.UserService.Infrastructure.Messaging;

using PetAdoption.UserService.Domain.Events;

public static class UserRabbitMqTopology
{
    public static class Exchanges
    {
        public const string UserEvents = "user.events";
    }

    public static string GetRoutingKey(DomainEventBase domainEvent) => domainEvent switch
    {
        UserRegisteredEvent => "user.registered.v1",
        UserProfileUpdatedEvent => "user.profile-updated.v1",
        UserSuspendedEvent => "user.suspended.v1",
        UserPasswordChangedEvent => "user.password-changed.v1",
        UserRoleChangedEvent => "user.role-changed.v1",
        _ => throw new ArgumentException($"Unknown event type {domainEvent.GetType()}")
    };
}
