using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;
using PetAdoption.PetService.Domain;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/adoption-requests")]
[Authorize]
public class AdoptionRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdoptionRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    /// <summary>
    /// Create an adoption request for a pet. User must be authenticated.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdoptionRequest([FromBody] CreateAdoptionRequestBody body)
    {
        var result = await _mediator.Send(new CreateAdoptionRequestCommand(
            GetUserId(), body.PetId, body.Message));
        return StatusCode(201, result);
    }

    /// <summary>
    /// Get the current user's adoption requests.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyAdoptionRequests(
        [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _mediator.Send(new GetMyAdoptionRequestsQuery(GetUserId(), skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Get adoption requests for an organization. Caller must be org admin/moderator.
    /// Organization membership is validated by the caller (Blazor frontend checks via UserService).
    /// </summary>
    [HttpGet("organization/{organizationId:guid}")]
    public async Task<IActionResult> GetOrgAdoptionRequests(
        Guid organizationId,
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        AdoptionRequestStatus? statusFilter = status is not null
            ? Enum.Parse<AdoptionRequestStatus>(status, ignoreCase: true)
            : null;

        var result = await _mediator.Send(new GetOrgAdoptionRequestsQuery(
            organizationId, statusFilter, skip, take));
        return Ok(result);
    }

    /// <summary>
    /// Approve an adoption request. Caller must be org admin/moderator.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveAdoptionRequest(Guid id)
    {
        var result = await _mediator.Send(new ApproveAdoptionRequestCommand(id, GetUserId()));
        return Ok(result);
    }

    /// <summary>
    /// Reject an adoption request. Caller must be org admin/moderator.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectAdoptionRequest(Guid id, [FromBody] RejectAdoptionRequestBody body)
    {
        var result = await _mediator.Send(new RejectAdoptionRequestCommand(id, GetUserId(), body.Reason));
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
