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
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetCallerId() =>
        Guid.Parse(User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("userId claim not found"));

    private Guid? GetCallerOrgId() =>
        Guid.TryParse(User.FindFirstValue("organizationId"), out var id) ? id : null;

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
            new GetChatHistoryQuery(requestId, afterId, take, GetCallerId(), GetCallerRole(), GetCallerOrgId()),
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
            new SendChatMessageCommand(requestId, request.Body, GetCallerId(), GetCallerRole(), GetCallerOrgId()),
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
            new MarkChatThreadReadCommand(requestId, GetCallerId(), GetCallerRole(), GetCallerOrgId()),
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Get total unread chat message count for the authenticated user.
    /// </summary>
    [HttpGet("api/me/chat/unread-total")]
    public async Task<IActionResult> GetMyUnreadTotal(CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyChatUnreadTotalQuery(GetCallerId()), ct);
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
