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
        return await _users.Find(u => u.Id.Value == id.Value).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _users.Find(u => u.Email.Value == email.Value).FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsWithEmailAsync(Email email)
    {
        return await _users.Find(u => u.Email.Value == email.Value).AnyAsync();
    }

    public async Task SaveAsync(User user)
    {
        // Save user (upsert)
        var filter = Builders<User>.Filter.Eq(u => u.Id.Value, user.Id.Value);
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
