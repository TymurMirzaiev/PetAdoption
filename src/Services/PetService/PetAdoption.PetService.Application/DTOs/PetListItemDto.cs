namespace PetAdoption.PetService.Application.DTOs;

public record PetListItemDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    string? Breed,
    int? AgeMonths,
    string? Description,
    List<string> Tags,
    string? PrimaryPhotoUrl = null,
    int MediaCount = 0
);
