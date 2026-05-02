using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Domain;

public class Vaccination
{
    public string VaccineType { get; private set; } = null!;
    public DateOnly AdministeredOn { get; private set; }
    public DateOnly? NextDueOn { get; private set; }
    public string? Notes { get; private set; }

    private Vaccination() { }

    internal static Vaccination Create(string vaccineType, DateOnly administeredOn, DateOnly? nextDueOn, string? notes)
    {
        if (string.IsNullOrWhiteSpace(vaccineType))
            throw new DomainException(
                PetDomainErrorCode.InvalidVaccination,
                "Vaccine type cannot be empty.");

        var trimmedType = vaccineType.Trim();
        if (trimmedType.Length > 100)
            throw new DomainException(
                PetDomainErrorCode.InvalidVaccination,
                $"Vaccine type cannot exceed 100 characters. Got {trimmedType.Length}.");

        if (nextDueOn.HasValue && nextDueOn.Value < administeredOn)
            throw new DomainException(
                PetDomainErrorCode.InvalidVaccination,
                "Next due date cannot be before administered date.");

        if (notes is not null && notes.Length > 500)
            throw new DomainException(
                PetDomainErrorCode.InvalidVaccination,
                $"Vaccination notes cannot exceed 500 characters. Got {notes.Length}.");

        return new Vaccination
        {
            VaccineType = trimmedType,
            AdministeredOn = administeredOn,
            NextDueOn = nextDueOn,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }
}
