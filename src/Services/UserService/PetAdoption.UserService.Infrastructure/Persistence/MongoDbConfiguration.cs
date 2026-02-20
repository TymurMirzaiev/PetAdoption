namespace PetAdoption.UserService.Infrastructure.Persistence;

using MongoDB.Bson.Serialization;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Events;
using PetAdoption.UserService.Infrastructure.Persistence.Serializers;

public static class MongoDbConfiguration
{
    private static bool _isConfigured = false;
    private static readonly object _lock = new object();

    public static void Configure()
    {
        if (_isConfigured) return;

        lock (_lock)
        {
            if (_isConfigured) return;

            // Register custom serializers for value objects
            BsonSerializer.RegisterSerializer(new UserIdSerializer());
            BsonSerializer.RegisterSerializer(new EmailSerializer());
            BsonSerializer.RegisterSerializer(new FullNameSerializer());
            BsonSerializer.RegisterSerializer(new PasswordSerializer());
            BsonSerializer.RegisterSerializer(new PhoneNumberSerializer());

            // Configure User entity
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id);
                cm.UnmapMember(x => x.DomainEvents); // Don't persist domain events
                cm.SetIgnoreExtraElements(true);
            });

            // Configure domain events
            BsonClassMap.RegisterClassMap<DomainEventBase>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
                cm.SetIgnoreExtraElements(true);
            });

            BsonClassMap.RegisterClassMap<UserRegisteredEvent>();
            BsonClassMap.RegisterClassMap<UserProfileUpdatedEvent>();
            BsonClassMap.RegisterClassMap<UserPasswordChangedEvent>();
            BsonClassMap.RegisterClassMap<UserRoleChangedEvent>();
            BsonClassMap.RegisterClassMap<UserSuspendedEvent>();

            _isConfigured = true;
        }
    }
}
