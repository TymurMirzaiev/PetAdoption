namespace PetAdoption.UserService.Domain.Exceptions;

public class UserSuspendedException : DomainException
{
    public UserSuspendedException(string userId)
        : base("USER_SUSPENDED", $"User '{userId}' is suspended and cannot login")
    {
    }
}
