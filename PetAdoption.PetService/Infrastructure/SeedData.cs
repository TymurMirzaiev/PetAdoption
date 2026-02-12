using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public static class SeedData
{
    public static IEnumerable<Pet> GetSeedPets() => new[]
    {
        new Pet { Id = Guid.NewGuid(), Name = "Bella", Type = "Dog", Status = PetStatus.Available },
        new Pet { Id = Guid.NewGuid(), Name = "Max", Type = "Cat", Status = PetStatus.Available },
        new Pet { Id = Guid.NewGuid(), Name = "Luna", Type = "Rabbit", Status = PetStatus.Available }
    };
}
