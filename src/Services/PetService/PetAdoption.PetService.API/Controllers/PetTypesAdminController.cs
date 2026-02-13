using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/admin/pet-types")]
public class PetTypesAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetTypesAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/admin/pet-types
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PetTypeDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var petTypes = await _mediator.Send(new GetAllPetTypesQuery(includeInactive));
        return Ok(petTypes);
    }

    // GET /api/admin/pet-types/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PetTypeDto>> GetById(Guid id)
    {
        var petType = await _mediator.Send(new GetPetTypeByIdQuery(id));
        return Ok(petType);
    }

    // POST /api/admin/pet-types
    [HttpPost]
    public async Task<ActionResult<CreatePetTypeResponse>> Create(CreatePetTypeRequest request)
    {
        var result = await _mediator.Send(new CreatePetTypeCommand(request.Code, request.Name));
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // PUT /api/admin/pet-types/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<UpdatePetTypeResponse>> Update(Guid id, UpdatePetTypeRequest request)
    {
        var result = await _mediator.Send(new UpdatePetTypeCommand(id, request.Name));
        return Ok(result);
    }

    // POST /api/admin/pet-types/{id}/deactivate
    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult<DeactivatePetTypeResponse>> Deactivate(Guid id)
    {
        var result = await _mediator.Send(new DeactivatePetTypeCommand(id));
        return Ok(result);
    }

    // POST /api/admin/pet-types/{id}/activate
    [HttpPost("{id}/activate")]
    public async Task<ActionResult<ActivatePetTypeResponse>> Activate(Guid id)
    {
        var result = await _mediator.Send(new ActivatePetTypeCommand(id));
        return Ok(result);
    }
}

public record CreatePetTypeRequest(string Code, string Name);
public record UpdatePetTypeRequest(string Name);
