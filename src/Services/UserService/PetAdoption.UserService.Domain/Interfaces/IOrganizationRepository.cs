using PetAdoption.UserService.Domain.Entities;

namespace PetAdoption.UserService.Domain.Interfaces;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id);
    Task<IEnumerable<Organization>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<Organization?> GetBySlugAsync(string slug);
    Task<(IEnumerable<Organization> Items, long Total)> GetAllAsync(int skip, int take);
    Task AddAsync(Organization organization);
    Task UpdateAsync(Organization organization);
}
