using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetAdoption.PetService.API.Constants;

namespace PetAdoption.PetService.API.Authorization;

/// <summary>
/// Action filter that validates the authenticated user is a member of the organization
/// specified in the route parameter {orgId} with OrgAdmin or OrgModerator role.
/// Expects JWT claims: "organizationId" and "orgRole".
/// </summary>
public class OrgAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get orgId from route
        if (!context.RouteData.Values.TryGetValue("orgId", out var orgIdValue) ||
            !Guid.TryParse(orgIdValue?.ToString(), out var routeOrgId))
        {
            context.Result = new BadRequestObjectResult(new { error = "Invalid organization ID in route" });
            return;
        }

        // Check user's org membership from JWT claims
        var userOrgIdClaim = user.FindFirst(ClaimNames.OrganizationId)?.Value;
        var userOrgRoleClaim = user.FindFirst(ClaimNames.OrgRole)?.Value;

        if (string.IsNullOrEmpty(userOrgIdClaim) ||
            !Guid.TryParse(userOrgIdClaim, out var userOrgId) ||
            userOrgId != routeOrgId)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Check org role (Admin or Moderator)
        if (string.IsNullOrEmpty(userOrgRoleClaim) ||
            (userOrgRoleClaim != "Admin" && userOrgRoleClaim != "Moderator"))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
