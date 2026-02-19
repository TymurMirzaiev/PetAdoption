namespace PetAdoption.UserService.Domain.Exceptions;

public class DuplicateEmailException : DomainException
{
    public DuplicateEmailException(string email)
        : base("DUPLICATE_EMAIL", $"User with email '{email}' already exists")
    {
    }
}
