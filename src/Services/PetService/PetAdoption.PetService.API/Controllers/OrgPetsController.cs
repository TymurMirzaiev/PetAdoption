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
public class OrgPetsController : PetServiceControllerBase
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
        take = Math.Min(take, 100);

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
    public async Task<IActionResult> Delete(Guid orgId, Guid petId)
    {
        await _mediator.Send(new DeleteOrgPetCommand(orgId, petId));
        return NoContent();
    }

    // POST /api/organizations/{orgId}/pets/{petId}/media
    [HttpPost("{petId}/media")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<UploadPetMediaResponse>> UploadMedia(
        Guid orgId, Guid petId, IFormFile file)
    {
        var result = await _mediator.Send(new UploadPetMediaCommand(
            orgId, petId, file.OpenReadStream(), file.ContentType, file.FileName,
            GetOrganizationId(), GetOrgRole()));

        return Ok(result);
    }

    // DELETE /api/organizations/{orgId}/pets/{petId}/media/{mediaId}
    [HttpDelete("{petId}/media/{mediaId}")]
    public async Task<IActionResult> DeleteMedia(Guid orgId, Guid petId, Guid mediaId)
    {
        await _mediator.Send(new DeletePetMediaCommand(
            orgId, petId, mediaId, GetOrganizationId(), GetOrgRole()));

        return NoContent();
    }

    // PUT /api/organizations/{orgId}/pets/{petId}/media/order
    [HttpPut("{petId}/media/order")]
    public async Task<IActionResult> ReorderMedia(
        Guid orgId, Guid petId, [FromBody] ReorderPetPhotosRequest request)
    {
        await _mediator.Send(new ReorderPetPhotosCommand(
            orgId, petId, request.OrderedIds, GetOrganizationId(), GetOrgRole()));

        return NoContent();
    }

    // PUT /api/organizations/{orgId}/pets/{petId}/media/{mediaId}/primary
    [HttpPut("{petId}/media/{mediaId}/primary")]
    public async Task<IActionResult> SetPrimaryMedia(Guid orgId, Guid petId, Guid mediaId)
    {
        await _mediator.Send(new SetPrimaryPhotoCommand(
            orgId, petId, mediaId, GetOrganizationId(), GetOrgRole()));

        return NoContent();
    }

    // PUT /api/organizations/{orgId}/pets/{petId}/medical-record
    [HttpPut("{petId}/medical-record")]
    public async Task<ActionResult<UpdatePetMedicalRecordResponse>> UpdateMedicalRecord(
        Guid orgId, Guid petId, [FromBody] UpdatePetMedicalRecordRequest request)
    {
        var vaccinationInputs = (request.Vaccinations ?? [])
            .Select(v => new VaccinationInput(v.VaccineType, v.AdministeredOn, v.NextDueOn, v.Notes));

        var result = await _mediator.Send(new UpdatePetMedicalRecordCommand(
            orgId, petId,
            request.IsSpayedNeutered, request.SpayNeuterDate, request.MicrochipId,
            request.HistoryNotes, request.LastVetVisit,
            vaccinationInputs,
            request.Allergies ?? [],
            GetOrganizationId(), GetOrgRole()));

        return Ok(result);
    }
}

public record CreateOrgPetRequest(string Name, Guid PetTypeId, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record UpdateOrgPetRequest(string Name, string? Breed = null, int? AgeMonths = null, string? Description = null, List<string>? Tags = null);
public record ReorderPetPhotosRequest(IReadOnlyList<Guid> OrderedIds);
public record UpdatePetMedicalRecordRequest(
    bool IsSpayedNeutered,
    DateOnly? SpayNeuterDate,
    string? MicrochipId,
    string? HistoryNotes,
    DateOnly? LastVetVisit,
    List<VaccinationInputDto>? Vaccinations,
    List<string>? Allergies);
public record VaccinationInputDto(string VaccineType, DateOnly AdministeredOn, DateOnly? NextDueOn, string? Notes);
