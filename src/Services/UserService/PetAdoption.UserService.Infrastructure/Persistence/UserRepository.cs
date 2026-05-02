namespace PetAdoption.UserService.Infrastructure.Persistence;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;
using PetAdoption.UserService.Infrastructure.Messaging;

public class UserRepository : RepositoryBase, IUserRepository
{
    public UserRepository(UserServiceDbContext db) : base(db)
    {
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
        await UpsertAsync(_db.Users, user, u => u.Id == user.Id);

        // Save domain events to outbox
        foreach (var domainEvent in user.DomainEvents)
        {
            var outboxEvent = new OutboxEvent
            {
                EventType = domainEvent.GetType().Name,
                EventData = JsonSerializer.Serialize(domainEvent),
                RoutingKey = UserRabbitMqTopology.GetRoutingKey(domainEvent),
                CreatedAt = DateTime.UtcNow
            };

            _db.OutboxEvents.Add(outboxEvent);
        }

        await _db.SaveChangesAsync();
        user.ClearDomainEvents();
    }
}
