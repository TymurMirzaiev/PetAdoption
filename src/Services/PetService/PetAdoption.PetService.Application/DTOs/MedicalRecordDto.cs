namespace PetAdoption.PetService.Application.DTOs;

public record MedicalRecordDto(
    bool IsSpayedNeutered,
    DateOnly? SpayNeuterDate,
    string? MicrochipId,
    string? History,
    DateOnly? LastVetVisit,
    IReadOnlyList<VaccinationDto> Vaccinations,
    IReadOnlyList<string> Allergies,
    DateTime UpdatedAt);

public record VaccinationDto(string VaccineType, DateOnly AdministeredOn, DateOnly? NextDueOn, string? Notes);
