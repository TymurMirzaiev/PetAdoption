using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/discover")]
[Authorize]
public class DiscoverController : PetServiceControllerBase
{
    private readonly IMediator _mediator;

    public DiscoverController(IMediator mediator)
    {
        _mediator = mediator;
    }

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
        [FromQuery] string? breed = null,
        [FromQuery] decimal? lat = null,
        [FromQuery] decimal? lng = null,
        [FromQuery] int? radiusKm = null)
    {
        take = Math.Min(take, 50);

        var result = await _mediator.Send(new GetDiscoverPetsQuery(
            GetUserId(),
            petTypeId,
            minAge,
            maxAge,
            take,
            breed,
            lat,
            lng,
            radiusKm));

        return Ok(result);
    }
}
