namespace PetAdoption.UserService.Infrastructure.Persistence;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Domain.Events;

public class UserRepository : IUserRepository
{
    private readonly UserServiceDbContext _db;

    public UserRepository(UserServiceDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(UserId id)
    {
        return await _db.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsWithEmailAsync(Email email)
    {
        return await _db.Users.AnyAsync(u => u.Email == email);
    }

    public async Task SaveAsync(User user)
    {
        var entry = _db.Entry(user);
        if (entry.State == EntityState.Detached)
        {
            var exists = await _db.Users.AnyAsync(u => u.Id == user.Id);
            if (exists)
                _db.Users.Update(user);
            else
                _db.Users.Add(user);
        }

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

            _db.OutboxEvents.Add(outboxEvent);
        }

        await _db.SaveChangesAsync();
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
