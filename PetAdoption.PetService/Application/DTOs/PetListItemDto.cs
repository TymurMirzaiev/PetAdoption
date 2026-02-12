namespace PetAdoption.PetService.Application.DTOs;

public record PetListItemDto(
    Guid Id,
    string Name,
    string Type,
    string Status
);
