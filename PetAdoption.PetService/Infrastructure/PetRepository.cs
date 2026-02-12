using System.Text.Json;
using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure;

public class PetRepository : IPetRepository
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<OutboxEvent> _outboxEvents;

    public PetRepository(IConfiguration configuration)
    {
        _client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        _database = _client.GetDatabase("PetAdoptionDb");
        _pets = _database.GetCollection<Pet>("Pets");
        _outboxEvents = _database.GetCollection<OutboxEvent>("OutboxEvents");
    }

    public async Task<Pet?> GetById(Guid id)
    {
        return await _pets.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task Add(Pet pet)
    {
        await SaveAggregateWithEvents(pet, isNew: true);
    }

    public async Task Update(Pet pet)
    {
        await SaveAggregateWithEvents(pet, isNew: false);
    }

    private async Task SaveAggregateWithEvents(Pet aggregate, bool isNew)
    {
        using var session = await _client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            if (isNew)
            {
                await _pets.InsertOneAsync(session, aggregate);
            }
            else
            {
                var currentVersion = aggregate.Version;
                aggregate.IncrementVersion();

                var result = await _pets.ReplaceOneAsync(
                    session,
                    p => p.Id == aggregate.Id && p.Version == currentVersion,
                    aggregate);

                if (result.MatchedCount == 0)
                {
                    throw new DomainException(
                        PetDomainErrorCode.ConcurrencyConflict,
                        $"Pet {aggregate.Id} was modified by another operation. Please reload and try again.",
                        new Dictionary<string, object>
                        {
                            { "PetId", aggregate.Id },
                            { "ExpectedVersion", currentVersion }
                        });
                }
            }

            if (aggregate.DomainEvents.Any())
            {
                var outboxEvents = aggregate.DomainEvents
                    .Select(SerializeEvent)
                    .ToList();

                await _outboxEvents.InsertManyAsync(session, outboxEvents);
            }

            await session.CommitTransactionAsync();

            aggregate.ClearDomainEvents();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    private static OutboxEvent SerializeEvent(IDomainEvent domainEvent)
    {
        var eventData = JsonSerializer.Serialize(
            domainEvent,
            domainEvent.GetType(),
            new JsonSerializerOptions { WriteIndented = false });

        return new OutboxEvent(domainEvent, eventData);
    }
}
