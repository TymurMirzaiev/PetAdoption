using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Mappings;

public static class PetMapper
{
    public static PetListItemDto ToPetListItemDto(Pet pet, string petTypeName) =>
        new(
            pet.Id,
            pet.Name,
            petTypeName,
            pet.Status.ToString(),
            pet.Breed?.Value,
            pet.Age?.Months,
            pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList());
}
