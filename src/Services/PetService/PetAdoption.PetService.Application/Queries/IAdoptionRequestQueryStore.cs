using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.Application.Queries;

public interface IAdoptionRequestQueryStore
{
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByUserAsync(Guid userId, int skip, int take);
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByOrganizationAsync(Guid organizationId, AdoptionRequestStatus? status, int skip, int take);
    Task<(IEnumerable<AdoptionRequest> Items, long Total)> GetByPetAsync(Guid petId, int skip, int take);
}
