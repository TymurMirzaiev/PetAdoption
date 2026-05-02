using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Constants;

namespace PetAdoption.PetService.API.Controllers;

public abstract class PetServiceControllerBase : ControllerBase
{
    protected Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimNames.UserId)?.Value
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    protected Guid? GetOrganizationId() =>
        Guid.TryParse(User.FindFirst(ClaimNames.OrganizationId)?.Value, out var id) ? id : null;

    protected string? GetOrgRole() => User.FindFirst(ClaimNames.OrgRole)?.Value;
}
