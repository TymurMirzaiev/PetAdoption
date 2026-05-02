using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/pet-types")]
[AllowAnonymous]
public class PetTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/pet-types — public endpoint, returns only active types
    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var petTypes = await _mediator.Send(new GetAllPetTypesQuery(IncludeInactive: false));
        return Ok(new { items = petTypes });
    }
}
