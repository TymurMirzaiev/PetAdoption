using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/discover")]
[Authorize]
public class DiscoverController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscoverController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Get personalized pet discovery feed. Returns Available pets the user
    /// hasn't liked or skipped yet, filtered by optional criteria.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Discover(
        [FromQuery] Guid? petTypeId = null,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] int take = 10,
        [FromQuery] string? breed = null)
    {
        var result = await _mediator.Send(new GetDiscoverPetsQuery(
            GetUserId(),
            petTypeId,
            minAge,
            maxAge,
            take,
            breed));

        return Ok(result);
    }
}
