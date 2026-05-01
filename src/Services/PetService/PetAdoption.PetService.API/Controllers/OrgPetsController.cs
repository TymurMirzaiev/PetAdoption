using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/organizations/{orgId}/pets")]
[Authorize]
[ServiceFilter(typeof(OrgAuthorizationFilter))]
public class OrgPetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrgPetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/organizations/{orgId}/pets?status=Available&tags=friendly,vaccinated&skip=0&take=20
    [HttpGet]
    public async Task<ActionResult<GetOrgPetsResponse>> GetAll(
        Guid orgId,
        [FromQuery] string? status = null,
        [FromQuery] string? tags = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        PetStatus? petStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PetStatus>(status, true, out var parsed))
            petStatus = parsed;

        IEnumerable<string>? tagList = null;
        if (!string.IsNullOrWhiteSpace(tags))
            tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = await _mediator.Send(new GetOrgPetsQuery(orgId, petStatus, skip, take, tagList));
        return Ok(result);
    }

    // POST /api/organizations/{orgId}/pets
    [HttpPost]
    public async Task<ActionResult<CreateOrgPetResponse>> Create(Guid orgId, CreateOrgPetRequest request)
    {
        var result = await _mediator.Send(new CreateOrgPetCommand(
            orgId, request.Name, request.PetTypeId, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return CreatedAtAction(nameof(GetAll), new { orgId }, result);
    }

    // PUT /api/organizations/{orgId}/pets/{petId}
    [HttpPut("{petId}")]
    public async Task<ActionResult<UpdateOrgPetResponse>> Update(Guid orgId, Guid petId, UpdateOrgPetRequest request)
    {
        var result = await _mediator.Send(new UpdateOrgPetCommand(
            orgId, petId, request.Name, request.Breed, request.AgeMonths, request.Description, request.Tags));
        return Ok(result);
    }

    // DELETE /api/organizations/{orgId}/pets/{petId}
    [HttpDelete("{petId}")]
    public async Task<ActionResult<DeleteOrgPetResponse>> Delete(Guid orgId, Guid petId)
    {
        var result = await _mediator.Send(new DeleteOrgPetCommand(orgId, petId));
        return Ok(result);
    }
}

public record CreateOrgPetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdateOrgPetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
