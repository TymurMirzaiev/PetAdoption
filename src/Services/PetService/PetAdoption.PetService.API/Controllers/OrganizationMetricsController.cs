using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class OrganizationMetricsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationMetricsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/organizations/{orgId:guid}/metrics")]
    [ServiceFilter(typeof(OrgAuthorizationFilter))]
    public async Task<IActionResult> GetOrgMetrics(
        Guid orgId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = true)
    {
        var result = await _mediator.Send(new GetOrgMetricsQuery(orgId, from, to, sortBy, descending));
        return Ok(result);
    }

    [HttpGet("api/pets/{petId:guid}/metrics")]
    public async Task<IActionResult> GetPetMetrics(
        Guid petId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        Guid? callerOrgId = Guid.TryParse(User.FindFirst("organizationId")?.Value, out var id) ? id : null;
        var callerOrgRole = User.FindFirst("orgRole")?.Value;
        var result = await _mediator.Send(new GetPetMetricsQuery(petId, from, to, callerOrgId, callerOrgRole));
        return Ok(result);
    }
}
