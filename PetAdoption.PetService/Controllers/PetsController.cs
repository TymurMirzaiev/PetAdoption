using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Infrastructure.Mediator;

namespace PetAdoption.PetService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/pets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PetListItemDto>>> GetAll()
    {
        var pets = await _mediator.Send(new GetAllPetsQuery());
        return Ok(pets);
    }

    // POST /api/pets/{id}/reserve
    [HttpPost("{id}/reserve")]
    public async Task<ActionResult<ReservePetResponse>> Reserve(Guid id)
    {
        var result = await _mediator.Send(new ReservePetCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
