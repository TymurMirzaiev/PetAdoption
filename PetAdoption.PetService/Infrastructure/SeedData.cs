using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Infrastructure;

public static class SeedData
{
    public static IEnumerable<Pet> GetSeedPets() => new[]
    {
        Pet.Create("Bella", "Dog"),
        Pet.Create("Max", "Cat"),
        Pet.Create("Luna", "Rabbit")
    };
}
