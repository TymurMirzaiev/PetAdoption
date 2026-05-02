using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Queries;

public record GetPetByIdQuery : IRequest<PetDetailsDto>
{
    public GetPetByIdQuery(Guid petId)
    {
        PetId = petId;
    }

    public Guid PetId { get; }
}

public record PetDetailsDto(
    Guid Id,
    string Name,
    string Type,
    string Status,
    string? Breed,
    int? AgeMonths,
    string? Description,
    List<string> Tags,
    MedicalRecordDto? MedicalRecord = null
);

public class GetPetByIdQueryHandler : IRequestHandler<GetPetByIdQuery, PetDetailsDto>
{
    private readonly IPetQueryStore _queryStore;
    private readonly IPetTypeRepository _petTypeRepository;

    public GetPetByIdQueryHandler(IPetQueryStore queryStore, IPetTypeRepository petTypeRepository)
    {
        _queryStore = queryStore;
        _petTypeRepository = petTypeRepository;
    }

    public async Task<PetDetailsDto> Handle(GetPetByIdQuery request, CancellationToken cancellationToken = default)
    {
        var pet = await _queryStore.GetById(request.PetId);
        if (pet == null)
        {
            throw new DomainException(
                PetDomainErrorCode.PetNotFound,
                $"Pet with ID {request.PetId} was not found.",
                new Dictionary<string, object>
                {
                    { "PetId", request.PetId }
                });
        }

        // Fetch pet type
        var petType = await _petTypeRepository.GetByIdAsync(pet.PetTypeId, cancellationToken);
        var petTypeName = petType?.Name ?? "Unknown";

        MedicalRecordDto? medicalRecordDto = null;
        if (pet.MedicalRecord is not null)
        {
            var mr = pet.MedicalRecord;
            medicalRecordDto = new MedicalRecordDto(
                mr.IsSpayedNeutered,
                mr.SpayNeuterDate,
                mr.MicrochipId?.Value,
                mr.History?.Value,
                mr.LastVetVisit,
                mr.Vaccinations.Select(v => new VaccinationDto(
                    v.VaccineType, v.AdministeredOn, v.NextDueOn, v.Notes)).ToList(),
                mr.Allergies.Select(a => a.Value).ToList(),
                mr.UpdatedAt);
        }

        return new PetDetailsDto(
            pet.Id,
            pet.Name,
            petTypeName,
            pet.Status.ToString(),
            pet.Breed?.Value,
            pet.Age?.Months,
            pet.Description?.Value,
            pet.Tags.Select(t => t.Value).ToList(),
            medicalRecordDto
        );
    }
}
