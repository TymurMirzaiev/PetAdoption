using Microsoft.Extensions.Logging;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Infrastructure.Persistence;

/// <summary>
/// Seeds development data: 10 pets per organization with varied types, breeds, and ages.
/// Only runs in Development environment. Idempotent — skips if pets already exist.
/// </summary>
public class DevDataSeeder
{
    private readonly IPetRepository _petRepository;
    private readonly IPetQueryStore _petQueryStore;
    private readonly IPetTypeRepository _petTypeRepository;
    private readonly ILogger<DevDataSeeder> _logger;

    // Deterministic org IDs — must match UserService DevDataSeeder
    private static readonly Guid HappyPawsOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CityRescueOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CountrysideHavenOrgId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public DevDataSeeder(
        IPetRepository petRepository,
        IPetQueryStore petQueryStore,
        IPetTypeRepository petTypeRepository,
        ILogger<DevDataSeeder> logger)
    {
        _petRepository = petRepository;
        _petQueryStore = petQueryStore;
        _petTypeRepository = petTypeRepository;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Checking if dev pet data needs to be seeded...");

        // Idempotency check: if any pets exist, skip
        var existing = await _petQueryStore.GetAll();
        if (existing.Any())
        {
            _logger.LogInformation("Pets already exist. Skipping dev seed.");
            return;
        }

        _logger.LogInformation("Seeding dev pet data...");

        // Load pet type IDs by code
        var petTypes = await _petTypeRepository.GetAllAsync();
        var typeMap = petTypes.ToDictionary(pt => pt.Code, pt => pt.Id);

        if (!typeMap.ContainsKey("dog") || !typeMap.ContainsKey("cat"))
        {
            _logger.LogWarning("Required pet types (dog, cat) not found. Run PetTypeSeeder first.");
            return;
        }

        await SeedHappyPawsPetsAsync(typeMap);
        await SeedCityRescuePetsAsync(typeMap);
        await SeedCountrysideHavenPetsAsync(typeMap);

        _logger.LogInformation("Dev pet data seeded successfully (30 pets total).");
    }

    private static Pet CreateOrgPet(string name, Guid petTypeId, string breed, int ageMonths, string description, Guid orgId)
    {
        var pet = Pet.Create(name, petTypeId, breed, ageMonths, description);
        pet.AssignToOrganization(orgId);
        return pet;
    }

    private async Task SeedHappyPawsPetsAsync(Dictionary<string, Guid> typeMap)
    {
        var orgId = HappyPawsOrgId;

        var pets = new[]
        {
            // Dogs (5)
            CreateOrgPet("Buddy", typeMap["dog"], "Labrador Retriever", 24,
                "Friendly and energetic lab who loves fetching tennis balls and swimming in the lake", orgId),
            CreateOrgPet("Luna", typeMap["dog"], "German Shepherd", 18,
                "Intelligent and loyal shepherd, great with kids and fully house-trained", orgId),
            CreateOrgPet("Charlie", typeMap["dog"], "Beagle", 36,
                "Sweet-natured beagle with the cutest howl, enjoys long walks and belly rubs", orgId),
            CreateOrgPet("Daisy", typeMap["dog"], "Golden Retriever", 12,
                "Playful golden puppy still learning her manners but overflowing with love", orgId),
            CreateOrgPet("Rocky", typeMap["dog"], "Husky", 48,
                "Majestic husky with striking blue eyes, needs an active family who loves winter", orgId),
            // Cats (3)
            CreateOrgPet("Whiskers", typeMap["cat"], "Persian", 60,
                "Elegant and calm Persian who enjoys lounging on sunny windowsills", orgId),
            CreateOrgPet("Mittens", typeMap["cat"], "Siamese", 30,
                "Vocal and affectionate Siamese who will greet you at the door every day", orgId),
            CreateOrgPet("Shadow", typeMap["cat"], "Maine Coon", 42,
                "Gentle giant Maine Coon, loves to be brushed and purrs like a motorboat", orgId),
            // Other (2)
            CreateOrgPet("Thumper", typeMap["rabbit"], "Holland Lop", 14,
                "Adorable lop-eared bunny who enjoys being held and loves fresh carrots", orgId),
            CreateOrgPet("Sunny", typeMap["bird"], "Cockatiel", 8,
                "Cheerful cockatiel who whistles tunes and loves sitting on your shoulder", orgId),
        };

        foreach (var pet in pets)
            await _petRepository.Add(pet);

        _logger.LogInformation("Seeded 10 pets for Happy Paws Shelter");
    }

    private async Task SeedCityRescuePetsAsync(Dictionary<string, Guid> typeMap)
    {
        var orgId = CityRescueOrgId;

        var pets = new[]
        {
            // Dogs (4)
            CreateOrgPet("Max", typeMap["dog"], "Poodle", 36,
                "Hypoallergenic standard poodle, smart and eager to please, loves agility courses", orgId),
            CreateOrgPet("Bella", typeMap["dog"], "Boxer", 24,
                "Energetic boxer with a heart of gold, great guard dog who is a total softie", orgId),
            CreateOrgPet("Cooper", typeMap["dog"], "Dachshund", 60,
                "Spirited little sausage dog who thinks he is a Great Dane, very brave", orgId),
            CreateOrgPet("Sadie", typeMap["dog"], "Border Collie", 15,
                "Brilliant border collie puppy who already knows five tricks", orgId),
            // Cats (4)
            CreateOrgPet("Oliver", typeMap["cat"], "Tabby", 18,
                "Classic orange tabby with a laid-back attitude, gets along with other cats", orgId),
            CreateOrgPet("Cleo", typeMap["cat"], "Russian Blue", 24,
                "Elegant Russian Blue with emerald eyes, a bit shy but incredibly sweet once bonded", orgId),
            CreateOrgPet("Milo", typeMap["cat"], "Bengal", 12,
                "Playful Bengal kitten with stunning spotted coat, needs lots of stimulation", orgId),
            CreateOrgPet("Nala", typeMap["cat"], "Ragdoll", 36,
                "Floppy and docile ragdoll who goes limp when you pick her up, pure bliss", orgId),
            // Other (2)
            CreateOrgPet("Nibbles", typeMap["hamster"], "Syrian Hamster", 6,
                "Curious Syrian hamster who loves running on his wheel and stuffing his cheeks", orgId),
            CreateOrgPet("Kiwi", typeMap["bird"], "Budgerigar", 10,
                "Bright green budgie who chatters all day and is learning to say hello", orgId),
        };

        foreach (var pet in pets)
            await _petRepository.Add(pet);

        _logger.LogInformation("Seeded 10 pets for City Animal Rescue");
    }

    private async Task SeedCountrysideHavenPetsAsync(Dictionary<string, Guid> typeMap)
    {
        var orgId = CountrysideHavenOrgId;

        var pets = new[]
        {
            // Dogs (4)
            CreateOrgPet("Bear", typeMap["dog"], "Bernese Mountain Dog", 30,
                "Gentle giant who loves the countryside, great with children and other animals", orgId),
            CreateOrgPet("Rosie", typeMap["dog"], "Cocker Spaniel", 48,
                "Happy-go-lucky spaniel with the waggiest tail, adores mud puddles", orgId),
            CreateOrgPet("Duke", typeMap["dog"], "Rottweiler", 36,
                "Loyal and protective rottie who is actually a big teddy bear at heart", orgId),
            CreateOrgPet("Willow", typeMap["dog"], "Australian Shepherd", 9,
                "Energetic Aussie puppy with mesmerizing merle coat, needs room to run", orgId),
            // Cats (3)
            CreateOrgPet("Pumpkin", typeMap["cat"], "Scottish Fold", 20,
                "Round-faced Scottish Fold with adorable folded ears, loves lap time", orgId),
            CreateOrgPet("Salem", typeMap["cat"], "Bombay", 42,
                "Sleek black Bombay cat with copper eyes, mysterious and cuddly in equal measure", orgId),
            CreateOrgPet("Ginger", typeMap["cat"], "Abyssinian", 15,
                "Active and curious Abyssinian who explores every corner and loves to climb", orgId),
            // Other (3)
            CreateOrgPet("Clover", typeMap["rabbit"], "Mini Rex", 10,
                "Velvety-soft Mini Rex rabbit who enjoys hopping around the garden", orgId),
            CreateOrgPet("Bubbles", typeMap["fish"], "Betta Fish", 6,
                "Stunning red and blue betta with flowing fins, a perfect desktop companion", orgId),
            CreateOrgPet("Hazel", typeMap["hamster"], "Dwarf Hamster", 3,
                "Tiny and adorable dwarf hamster, fits in the palm of your hand", orgId),
        };

        foreach (var pet in pets)
            await _petRepository.Add(pet);

        _logger.LogInformation("Seeded 10 pets for Countryside Haven");
    }
}
