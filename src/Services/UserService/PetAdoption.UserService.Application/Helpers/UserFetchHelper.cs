namespace PetAdoption.UserService.Application.Helpers;

using PetAdoption.UserService.Domain.Entities;
using PetAdoption.UserService.Domain.Exceptions;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Domain.ValueObjects;

public static class UserFetchHelper
{
    public static async Task<User> GetUserOrThrowAsync(IUserRepository userRepository, string rawUserId)
    {
        var userId = UserId.From(rawUserId);
        var user = await userRepository.GetByIdAsync(userId);
        if (user is null) throw new UserNotFoundException(rawUserId);
        return user;
    }
}
