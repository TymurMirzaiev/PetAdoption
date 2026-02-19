namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.ValueObjects;

public interface IUserQueryStore
{
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByEmailAsync(Email email);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 50);
    Task<int> CountAsync();
}
