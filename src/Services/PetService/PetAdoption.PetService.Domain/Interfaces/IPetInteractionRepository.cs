namespace PetAdoption.PetService.Domain.Interfaces;

public interface IPetInteractionRepository
{
    Task AddAsync(PetInteraction interaction);
    Task AddBatchAsync(IEnumerable<PetInteraction> interactions);
}
