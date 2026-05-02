using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Set or update the physical address of an organization.
    /// Requires the caller to be an Admin member of the organization.
    /// </summary>
    [HttpPost("api/organizations/{orgId:guid}/address")]
    [ServiceFilter(typeof(OrgAuthorizationFilter))]
    public async Task<IActionResult> SetAddress(
        Guid orgId,
        [FromBody] SetOrganizationAddressRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SetOrganizationAddressCommand(
            orgId,
            request.Lat,
            request.Lng,
            request.Line1,
            request.City,
            request.Region,
            request.Country,
            request.PostalCode,
            User.FindFirst("organizationId")?.Value ?? "",
            User.FindFirst("orgRole")?.Value ?? ""),
            ct);

        return Ok(result);
    }
}

public record SetOrganizationAddressRequest(
    decimal Lat,
    decimal Lng,
    string Line1,
    string City,
    string Region = "",
    string Country = "Unknown",
    string PostalCode = "");
