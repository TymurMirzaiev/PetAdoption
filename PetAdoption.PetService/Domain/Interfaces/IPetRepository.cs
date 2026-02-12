namespace PetAdoption.PetService.Domain.Interfaces;

public interface IPetRepository
{
    Task<IEnumerable<Pet>> GetAll();
    Task<Pet?> GetById(Guid id);
    Task Update(Pet pet);
}
