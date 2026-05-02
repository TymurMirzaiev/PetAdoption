namespace PetAdoption.UserService.Infrastructure.Messaging;

public static class UserRabbitMqTopology
{
    public static class Exchanges
    {
        public const string UserEvents = "user.events";
    }
}
