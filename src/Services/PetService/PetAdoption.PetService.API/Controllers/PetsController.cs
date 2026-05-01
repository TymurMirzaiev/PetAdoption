using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/pets?status=Available&petTypeId=...&skip=0&take=20&minAge=6&maxAge=24&breed=Golden
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<GetPetsResponse>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] Guid? petTypeId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] int? minAge = null,
        [FromQuery] int? maxAge = null,
        [FromQuery] string? breed = null)
    {
        PetStatus? petStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PetStatus>(status, true, out var parsed))
        {
            petStatus = parsed;
        }

        var result = await _mediator.Send(new GetPetsQuery(petStatus, petTypeId, skip, take, minAge, maxAge, breed));
        return Ok(result);
    }

    // POST /api/pets
    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<CreatePetResponse>> Create(CreatePetRequest request)
    {
        var result = await _mediator.Send(new CreatePetCommand(request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // GET /api/pets/{id}
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<PetDetailsDto>> GetById(Guid id)
    {
        var pet = await _mediator.Send(new GetPetByIdQuery(id));
        return Ok(pet);
    }

    // PUT /api/pets/{id}
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id}")]
    public async Task<ActionResult<UpdatePetResponse>> Update(Guid id, UpdatePetRequest request)
    {
        var result = await _mediator.Send(new UpdatePetCommand(id, request.Name, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return Ok(result);
    }

    // DELETE /api/pets/{id}
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<DeletePetResponse>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeletePetCommand(id));
        return Ok(result);
    }

    // POST /api/pets/{id}/reserve
    [Authorize]
    [HttpPost("{id}/reserve")]
    public async Task<ActionResult<ReservePetResponse>> Reserve(Guid id)
    {
        var result = await _mediator.Send(new ReservePetCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // POST /api/pets/{id}/adopt
    [Authorize]
    [HttpPost("{id}/adopt")]
    public async Task<ActionResult<AdoptPetResponse>> Adopt(Guid id)
    {
        var result = await _mediator.Send(new AdoptPetCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // POST /api/pets/{id}/cancel-reservation
    [Authorize]
    [HttpPost("{id}/cancel-reservation")]
    public async Task<ActionResult<CancelReservationResponse>> CancelReservation(Guid id)
    {
        var result = await _mediator.Send(new CancelReservationCommand(id));
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

public record CreatePetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdatePetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
