using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.API.Authorization;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Authorize]
public class ChatController : PetServiceControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Chat authorization uses the standard role claim (Admin/Moderator) rather than orgRole,
    // so it is kept as a local helper distinct from GetOrgRole().
    private string? GetCallerRole() => User.FindFirstValue(ClaimTypes.Role);

    /// <summary>
    /// Get paginated chat history for an adoption request thread.
    /// </summary>
    [HttpGet("api/adoption-requests/{requestId:guid}/messages")]
    public async Task<IActionResult> GetHistory(
        Guid requestId,
        [FromQuery] Guid? afterId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetChatHistoryQuery(requestId, afterId, take, GetUserId(), GetCallerRole(), GetOrganizationId()),
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Send a chat message in an adoption request thread.
    /// </summary>
    [HttpPost("api/adoption-requests/{requestId:guid}/messages")]
    public async Task<IActionResult> Send(
        Guid requestId,
        [FromBody] SendMessageRequest request,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new SendChatMessageCommand(requestId, request.Body, GetUserId(), GetCallerRole(), GetOrganizationId()),
            ct);
        return StatusCode(201, result.Message);
    }

    /// <summary>
    /// Mark all incoming messages in a thread as read for the caller.
    /// </summary>
    [HttpPost("api/adoption-requests/{requestId:guid}/messages/mark-read")]
    public async Task<IActionResult> MarkRead(Guid requestId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new MarkChatThreadReadCommand(requestId, GetUserId(), GetCallerRole(), GetOrganizationId()),
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Get total unread chat message count for the authenticated user.
    /// </summary>
    [HttpGet("api/me/chat/unread-total")]
    public async Task<IActionResult> GetMyUnreadTotal(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyChatUnreadTotalQuery(GetUserId()), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get total unread chat message count for an organization.
    /// </summary>
    [HttpGet("api/organizations/{orgId:guid}/chat/unread-total")]
    [ServiceFilter(typeof(OrgAuthorizationFilter))]
    public async Task<IActionResult> GetOrgUnreadTotal(Guid orgId, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOrgChatUnreadTotalQuery(orgId), ct);
        return Ok(result);
    }
}

public record SendMessageRequest(string Body);
