namespace PetAdoption.UserService.Domain.Exceptions;

public class OrganizationNotFoundException : DomainException
{
    public OrganizationNotFoundException(Guid organizationId)
        : base("ORGANIZATION_NOT_FOUND", $"Organization with ID '{organizationId}' not found")
    {
    }
}
