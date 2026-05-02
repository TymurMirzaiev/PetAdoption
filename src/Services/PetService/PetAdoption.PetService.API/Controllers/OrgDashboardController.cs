using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId:guid}")]
[Authorize]
[ServiceFilter(typeof(OrgAuthorizationFilter))]
public class OrgDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrgDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET api/organizations/{orgId}/dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<GetOrgDashboardResponse>> GetDashboard(Guid orgId)
    {
        var result = await _mediator.Send(new GetOrgDashboardQuery(orgId));
        return Ok(result);
    }

    // GET api/organizations/{orgId}/dashboard/trends?from=&to=
    [HttpGet("dashboard/trends")]
    public async Task<ActionResult<GetOrgDashboardTrendsResponse>> GetDashboardTrends(
        Guid orgId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _mediator.Send(new GetOrgDashboardTrendsQuery(orgId, from, to));
        return Ok(result);
    }
}
