using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class InteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    [HttpPost("api/pets/{petId:guid}/interactions")]
    public async Task<IActionResult> TrackInteraction(Guid petId, [FromBody] TrackInteractionRequest request)
    {
        if (!Enum.TryParse<InteractionType>(request.Type, true, out var type))
            return BadRequest(new { error = "Invalid interaction type. Use: Impression, Swipe, or Rejection." });

        var result = await _mediator.Send(new TrackInteractionCommand(petId, GetUserId(), type));
        return Ok(result);
    }

    [HttpPost("api/pets/interactions/batch")]
    public async Task<IActionResult> TrackBatchImpressions([FromBody] TrackBatchImpressionsRequest request)
    {
        if (request.PetIds is null || !request.PetIds.Any())
            return BadRequest(new { error = "PetIds cannot be empty." });

        var result = await _mediator.Send(new TrackBatchImpressionsCommand(request.PetIds, GetUserId()));
        return Ok(result);
    }
}

public record TrackInteractionRequest(string Type);
public record TrackBatchImpressionsRequest(IEnumerable<Guid> PetIds);
