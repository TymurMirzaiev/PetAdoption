namespace PetAdoption.UserService.Domain.Exceptions;

public class UserNotFoundException : DomainException
{
    public UserNotFoundException(string userId)
        : base("USER_NOT_FOUND", $"User with ID '{userId}' not found")
    {
    }
}
