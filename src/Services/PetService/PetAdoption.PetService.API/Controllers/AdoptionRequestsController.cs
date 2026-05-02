using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.API.Constants;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/adoption-requests")]
[Authorize]
public class AdoptionRequestsController : PetServiceControllerBase
{
    private readonly IMediator _mediator;

    public AdoptionRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create an adoption request for a pet. User must be authenticated.
    /// If Message is omitted or empty, falls back to the 'bio' JWT claim.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdoptionRequest([FromBody] CreateAdoptionRequestBody body)
    {
        var effectiveMessage = !string.IsNullOrEmpty(body.Message)
            ? body.Message
            : User.FindFirst(ClaimNames.Bio)?.Value;

        var result = await _mediator.Send(new CreateAdoptionRequestCommand(
            GetUserId(), body.PetId, effectiveMessage));
        return StatusCode(201, result);
    }

    /// <summary>
    /// Get the current user's adoption requests.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyAdoptionRequests(
        [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Min(take, 100);
        var result = await _mediator.Send(new GetMyAdoptionRequestsQuery(GetUserId(), skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Get adoption requests for an organization. Caller must be a member (Admin/Moderator) of that org.
    /// </summary>
    [HttpGet("organization/{orgId:guid}")]
    [ServiceFilter(typeof(OrgAuthorizationFilter))]
    public async Task<IActionResult> GetOrgAdoptionRequests(
        Guid orgId,
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        take = Math.Min(take, 100);

        AdoptionRequestStatus? statusFilter = null;
        if (status is not null)
        {
            if (!Enum.TryParse<AdoptionRequestStatus>(status, ignoreCase: true, out var parsedStatus))
                return BadRequest("Invalid status value.");
            statusFilter = parsedStatus;
        }

        var result = await _mediator.Send(new GetOrgAdoptionRequestsQuery(
            orgId, statusFilter, skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Approve an adoption request. Caller must be Admin/Moderator of the request's organization.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveAdoptionRequest(Guid id)
    {
        var result = await _mediator.Send(new ApproveAdoptionRequestCommand(
            id, GetUserId(), GetOrganizationId(), GetOrgRole()));
        return Ok(result);
    }

    /// <summary>
    /// Reject an adoption request. Caller must be Admin/Moderator of the request's organization.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectAdoptionRequest(Guid id, [FromBody] RejectAdoptionRequestBody body)
    {
        if (string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest("Reason is required.");

        var result = await _mediator.Send(new RejectAdoptionRequestCommand(
            id, GetUserId(), body.Reason, GetOrganizationId(), GetOrgRole()));
        return Ok(result);
    }

    /// <summary>
    /// Cancel an adoption request. Only the requesting user can cancel.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelAdoptionRequest(Guid id)
    {
        var result = await _mediator.Send(new CancelAdoptionRequestCommand(id, GetUserId()));
        return Ok(result);
    }
}

public record CreateAdoptionRequestBody(Guid PetId, string? Message);
public record RejectAdoptionRequestBody(string Reason);
