using Microsoft.Extensions.Logging;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with initial pet types.
/// </summary>
public class PetTypeSeeder
{
    private readonly IPetTypeRepository _repository;
    private readonly ILogger<PetTypeSeeder> _logger;

    public PetTypeSeeder(IPetTypeRepository repository, ILogger<PetTypeSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking if pet types need to be seeded...");

        var existingTypes = await _repository.GetAllAsync();
        if (existingTypes.Any())
        {
            _logger.LogInformation("Pet types already exist. Skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding initial pet types...");

        var initialTypes = new[]
        {
            PetType.Create("dog", "Dog"),
            PetType.Create("cat", "Cat"),
            PetType.Create("rabbit", "Rabbit"),
            PetType.Create("bird", "Bird"),
            PetType.Create("fish", "Fish"),
            PetType.Create("hamster", "Hamster")
        };

        foreach (var petType in initialTypes)
        {
            await _repository.AddAsync(petType);
            _logger.LogInformation("Seeded pet type: {Code} - {Name}", petType.Code, petType.Name);
        }

        _logger.LogInformation("Pet types seeded successfully.");
    }
}
