namespace PetAdoption.PetService.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static class Exchanges
    {
        public const string PetEvents = "pet.events";
        public const string PetDeadLetter = "pet.deadletter";
    }

    public static class Queues
    {
        public const string PetReservedNotifications = "pet.reserved.notifications";
        public const string PetAdoptedNotifications = "pet.adopted.notifications";
        public const string ReservationCancelledNotifications = "pet.reservation.cancelled.notifications";
    }

    public static class RoutingKeys
    {
        public const string PetReserved = "pet.reserved.v1";
        public const string PetAdopted = "pet.adopted.v1";
        public const string ReservationCancelled = "pet.reservation.cancelled.v1";
    }
}