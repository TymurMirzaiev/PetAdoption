namespace PetAdoption.PetService.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpsertAsync(Organization org, CancellationToken ct = default);
}
