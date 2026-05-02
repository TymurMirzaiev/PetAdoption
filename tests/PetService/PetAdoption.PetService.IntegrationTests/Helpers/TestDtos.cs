namespace PetAdoption.PetService.IntegrationTests.Helpers;

internal record CreatePetTypeResponseDto(Guid Id, string Code, string Name);

internal record PetTypeResponseDto(Guid Id, string Code, string Name, bool IsActive);
