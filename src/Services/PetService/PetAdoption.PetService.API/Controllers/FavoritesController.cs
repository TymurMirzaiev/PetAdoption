using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var result = await _mediator.Send(new AddFavoriteCommand(GetUserId(), request.PetId));
        return StatusCode(201, result);
    }

    [HttpDelete("{petId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid petId)
    {
        await _mediator.Send(new RemoveFavoriteCommand(GetUserId(), petId));
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetFavorites([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        var result = await _mediator.Send(new GetFavoritesQuery(GetUserId(), skip, take));
        return Ok(result);
    }
}

public record AddFavoriteRequest(Guid PetId);
