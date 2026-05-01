using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Application.Authorization;

internal static class OrgAuthorization
{
    public static void EnsureMember(Guid resourceOrgId, Guid? callerOrgId, string? callerOrgRole)
    {
        if (callerOrgId is null || callerOrgId.Value != resourceOrgId)
            throw new DomainException(
                PetDomainErrorCode.NotAuthorizedForOrg,
                "Caller is not a member of the target organization.",
                new Dictionary<string, object> { { "OrgId", resourceOrgId } });

        if (callerOrgRole is not "Admin" and not "Moderator")
            throw new DomainException(
                PetDomainErrorCode.NotAuthorizedForOrg,
                "Caller does not have an org role authorized for this action.",
                new Dictionary<string, object> { { "OrgId", resourceOrgId } });
    }
}
