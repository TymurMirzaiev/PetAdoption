namespace PetAdoption.UserService.Domain.Interfaces;

using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.ValueObjects;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id);
    Task<User?> GetByEmailAsync(Email email);
    Task<bool> ExistsWithEmailAsync(Email email);
    Task SaveAsync(User user);
}
