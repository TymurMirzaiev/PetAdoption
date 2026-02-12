using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public static class SeedData
{
    public static IEnumerable<Pet> GetSeedPets() => new[]
    {
        new Pet(Guid.NewGuid(), "Bella", "Dog"),
        new Pet(Guid.NewGuid(), "Max", "Cat"),
        new Pet(Guid.NewGuid(), "Luna", "Rabbit")
    };
}
