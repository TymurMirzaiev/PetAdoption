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

    // GET /api/pets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PetDetailsDto>> GetById(Guid id)
    {
        var pet = await _mediator.Send(new GetPetByIdQuery(id));
        return Ok(pet);
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

    // POST /api/pets/{id}/adopt
    [HttpPost("{id}/adopt")]
    public async Task<ActionResult<AdoptPetResponse>> Adopt(Guid id)
    {
        var result = await _mediator.Send(new AdoptPetCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // POST /api/pets/{id}/cancel-reservation
    [HttpPost("{id}/cancel-reservation")]
    public async Task<ActionResult<CancelReservationResponse>> CancelReservation(Guid id)
    {
        var result = await _mediator.Send(new CancelReservationCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
