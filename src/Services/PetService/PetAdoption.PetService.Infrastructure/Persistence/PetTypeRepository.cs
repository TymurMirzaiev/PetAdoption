using MongoDB.Driver;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

public class PetTypeRepository : IPetTypeRepository
{
    private readonly IMongoCollection<PetType> _petTypes;

    public PetTypeRepository(IMongoDatabase database)
    {
        _petTypes = database.GetCollection<PetType>("PetTypes");

        // Create unique index on Code
        var indexKeys = Builders<PetType>.IndexKeys.Ascending(pt => pt.Code);
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<PetType>(indexKeys, indexOptions);
        _petTypes.Indexes.CreateOneAsync(indexModel).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<PetType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _petTypes
            .Find(pt => pt.IsActive)
            .SortBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PetType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _petTypes
            .Find(_ => true)
            .SortBy(pt => pt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PetType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _petTypes
            .Find(pt => pt.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PetType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _petTypes
            .Find(pt => pt.Code == normalizedCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToLowerInvariant();
        return await _petTypes
            .Find(pt => pt.Code == normalizedCode)
            .AnyAsync(cancellationToken);
    }

    public async Task AddAsync(PetType petType, CancellationToken cancellationToken = default)
    {
        await _petTypes.InsertOneAsync(petType, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(PetType petType, CancellationToken cancellationToken = default)
    {
        await _petTypes.ReplaceOneAsync(
            pt => pt.Id == petType.Id,
            petType,
            cancellationToken: cancellationToken);
    }
}
