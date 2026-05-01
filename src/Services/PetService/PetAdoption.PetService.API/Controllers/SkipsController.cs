using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/skips")]
[Authorize]
public class SkipsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SkipsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Track a pet skip (user swiped left / dismissed the pet).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TrackSkip([FromBody] TrackSkipRequest request)
    {
        var result = await _mediator.Send(new TrackSkipCommand(GetUserId(), request.PetId));
        return StatusCode(201, result);
    }

    /// <summary>
    /// Reset all skips for the current user, allowing them to re-discover all pets.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> ResetSkips()
    {
        await _mediator.Send(new ResetSkipsCommand(GetUserId()));
        return NoContent();
    }
}

public record TrackSkipRequest(Guid PetId);
