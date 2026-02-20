namespace PetAdoption.UserService.Infrastructure.Persistence;

using System.Text.Json;
using MongoDB.Driver;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Events;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly IOutboxRepository _outboxRepository;

    public UserRepository(IMongoDatabase database, IOutboxRepository outboxRepository)
    {
        _users = database.GetCollection<User>("Users");
        _outboxRepository = outboxRepository;
    }

    public async Task<User?> GetByIdAsync(UserId id)
    {
        // Use Filter API instead of LINQ to work with custom serializers
        var filter = Builders<User>.Filter.Eq("_id", id.Value);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        // Use Filter API instead of LINQ to work with custom serializers
        var filter = Builders<User>.Filter.Eq("Email", email.Value);
        return await _users.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsWithEmailAsync(Email email)
    {
        // Use Filter API instead of LINQ to work with custom serializers
        var filter = Builders<User>.Filter.Eq("Email", email.Value);
        var count = await _users.CountDocumentsAsync(filter);
        return count > 0;
    }

    public async Task SaveAsync(User user)
    {
        // Save user (upsert) - Use Filter API to work with custom serializers
        var filter = Builders<User>.Filter.Eq("_id", user.Id.Value);
        await _users.ReplaceOneAsync(
            filter,
            user,
            new ReplaceOptions { IsUpsert = true }
        );

        // Save domain events to outbox
        foreach (var domainEvent in user.DomainEvents)
        {
            var outboxEvent = new OutboxEvent
            {
                EventType = domainEvent.GetType().Name,
                EventData = JsonSerializer.Serialize(domainEvent),
                RoutingKey = GetRoutingKey(domainEvent),
                CreatedAt = DateTime.UtcNow
            };

            await _outboxRepository.AddAsync(outboxEvent);
        }

        // Clear events after saving to outbox
        user.ClearDomainEvents();
    }

    private string GetRoutingKey(DomainEventBase domainEvent) => domainEvent switch
    {
        UserRegisteredEvent => "user.registered.v1",
        UserProfileUpdatedEvent => "user.profile-updated.v1",
        UserSuspendedEvent => "user.suspended.v1",
        UserPasswordChangedEvent => "user.password-changed.v1",
        UserRoleChangedEvent => "user.role-changed.v1",
        _ => throw new ArgumentException($"Unknown event type {domainEvent.GetType()}")
    };
}
