namespace PetAdoption.UserService.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base("INVALID_CREDENTIALS", "Invalid email or password")
    {
    }
}
