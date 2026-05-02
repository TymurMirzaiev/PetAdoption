using PetAdoption.PetService.Domain.ValueObjects;

namespace PetAdoption.PetService.Domain;

public record VaccinationInput(string VaccineType, DateOnly AdministeredOn, DateOnly? NextDueOn, string? Notes);

public class PetMedicalRecord
{
    public bool IsSpayedNeutered { get; private set; }
    public DateOnly? SpayNeuterDate { get; private set; }
    public MicrochipId? MicrochipId { get; private set; }
    public MedicalNotes? History { get; private set; }
    public DateOnly? LastVetVisit { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Vaccination> _vaccinations = new();
    private readonly List<Allergy> _allergies = new();

    public IReadOnlyList<Vaccination> Vaccinations => _vaccinations.AsReadOnly();
    public IReadOnlyList<Allergy> Allergies => _allergies.AsReadOnly();

    private PetMedicalRecord() { }

    internal static PetMedicalRecord Create(
        bool isSpayedNeutered,
        DateOnly? spayNeuterDate,
        string? microchipId,
        string? historyNotes,
        DateOnly? lastVetVisit,
        IEnumerable<VaccinationInput> vaccinations,
        IEnumerable<string> allergies)
    {
        var record = new PetMedicalRecord
        {
            IsSpayedNeutered = isSpayedNeutered,
            SpayNeuterDate = spayNeuterDate,
            MicrochipId = string.IsNullOrWhiteSpace(microchipId) ? null : new MicrochipId(microchipId),
            History = string.IsNullOrWhiteSpace(historyNotes) ? null : new MedicalNotes(historyNotes),
            LastVetVisit = lastVetVisit,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var v in vaccinations)
            record._vaccinations.Add(Vaccination.Create(v.VaccineType, v.AdministeredOn, v.NextDueOn, v.Notes));

        foreach (var a in allergies)
            record._allergies.Add(new Allergy(a));

        return record;
    }

    internal void Update(
        bool isSpayedNeutered,
        DateOnly? spayNeuterDate,
        string? microchipId,
        string? historyNotes,
        DateOnly? lastVetVisit,
        IEnumerable<VaccinationInput> vaccinations,
        IEnumerable<string> allergies)
    {
        IsSpayedNeutered = isSpayedNeutered;
        SpayNeuterDate = spayNeuterDate;
        MicrochipId = string.IsNullOrWhiteSpace(microchipId) ? null : new MicrochipId(microchipId);
        History = string.IsNullOrWhiteSpace(historyNotes) ? null : new MedicalNotes(historyNotes);
        LastVetVisit = lastVetVisit;
        UpdatedAt = DateTime.UtcNow;

        _vaccinations.Clear();
        foreach (var v in vaccinations)
            _vaccinations.Add(Vaccination.Create(v.VaccineType, v.AdministeredOn, v.NextDueOn, v.Notes));

        _allergies.Clear();
        foreach (var a in allergies)
            _allergies.Add(new Allergy(a));
    }
}
