using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
public class PetMediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetMediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/pets/{petId}/media
    [HttpGet("api/pets/{petId:guid}/media")]
    [AllowAnonymous]
    public async Task<ActionResult<GetPetMediaResponse>> GetMedia(Guid petId)
    {
        var result = await _mediator.Send(new GetPetMediaQuery(petId));
        return Ok(result);
    }
}
