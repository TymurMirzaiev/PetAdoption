using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Mappings;

public static class PetMapper
{
    public static PetListItemDto ToPetListItemDto(Pet pet, string petTypeName)
    {
        var primaryPhoto = pet.Media
            .Where(m => m.MediaType == PetMediaType.Photo)
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.SortOrder)
            .FirstOrDefault();

        return new(
            pet.Id,
            pet.Name,
            petTypeName,
            pet.Status.ToString(),
            pet.Breed?.Value,
            pet.Age?.Months,
            pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList(),
            primaryPhoto?.Url,
            pet.Media.Count);
    }
}
