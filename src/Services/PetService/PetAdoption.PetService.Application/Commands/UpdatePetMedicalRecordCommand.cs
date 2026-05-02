using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Authorization;
using PetAdoption.PetService.Domain;
using PetAdoption.PetService.Domain.Exceptions;
using PetAdoption.PetService.Domain.Interfaces;

namespace PetAdoption.PetService.Application.Commands;

public record UpdatePetMedicalRecordCommand(
    Guid OrgId,
    Guid PetId,
    bool IsSpayedNeutered,
    DateOnly? SpayNeuterDate,
    string? MicrochipId,
    string? HistoryNotes,
    DateOnly? LastVetVisit,
    IEnumerable<VaccinationInput> Vaccinations,
    IEnumerable<string> Allergies,
    Guid? ReviewerOrgId,
    string? ReviewerOrgRole) : IRequest<UpdatePetMedicalRecordResponse>;

public record UpdatePetMedicalRecordResponse(Guid PetId, DateTime UpdatedAt);

public class UpdatePetMedicalRecordCommandHandler
    : IRequestHandler<UpdatePetMedicalRecordCommand, UpdatePetMedicalRecordResponse>
{
    private readonly IPetRepository _petRepository;

    public UpdatePetMedicalRecordCommandHandler(IPetRepository petRepository)
    {
        _petRepository = petRepository;
    }

    public async Task<UpdatePetMedicalRecordResponse> Handle(
        UpdatePetMedicalRecordCommand request, CancellationToken ct = default)
    {
        var pet = await _petRepository.GetByIdOrThrowAsync(request.PetId);

        OrgAuthorization.EnsureMember(
            pet.OrganizationId ?? Guid.Empty, request.ReviewerOrgId, request.ReviewerOrgRole);

        pet.UpdateMedicalRecord(
            request.IsSpayedNeutered,
            request.SpayNeuterDate,
            request.MicrochipId,
            request.HistoryNotes,
            request.LastVetVisit,
            request.Vaccinations,
            request.Allergies);

        await _petRepository.Update(pet);

        return new UpdatePetMedicalRecordResponse(
            pet.Id,
            pet.MedicalRecord!.UpdatedAt);
    }
}
