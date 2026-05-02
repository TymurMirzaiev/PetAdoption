using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands.Organizations;
using PetAdoption.UserService.Application.Constants;
using PetAdoption.UserService.Application.Queries.Organizations;

namespace PetAdoption.UserService.API.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        [FromServices] ICommandHandler<CreateOrganizationCommand, CreateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new CreateOrganizationCommand(request.Name, request.Slug, request.Description));
        if (!result.Success) return Conflict(result);
        return Created($"/api/organizations/{result.OrganizationId}", result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromServices] IQueryHandler<GetOrganizationsQuery, GetOrganizationsResponse> handler = null!)
    {
        var result = await handler.HandleAsync(new GetOrganizationsQuery(skip, take));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse?> handler)
    {
        var result = await handler.HandleAsync(new GetOrganizationByIdQuery(id));
        if (result is null) return NotFound();
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        [FromServices] ICommandHandler<UpdateOrganizationCommand, UpdateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new UpdateOrganizationCommand(id, request.Name, request.Description));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] ICommandHandler<DeactivateOrganizationCommand, DeactivateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new DeactivateOrganizationCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromServices] ICommandHandler<ActivateOrganizationCommand, ActivateOrganizationResponse> handler)
    {
        var result = await handler.HandleAsync(new ActivateOrganizationCommand(id));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid id,
        [FromBody] AddMemberRequest request,
        [FromServices] ICommandHandler<AddOrganizationMemberCommand, AddOrganizationMemberResponse> handler)
    {
        var result = await handler.HandleAsync(new AddOrganizationMemberCommand(id, request.UserId, request.Role));
        if (!result.Success) return Conflict(result);
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(
        Guid id,
        [FromServices] IQueryHandler<GetOrganizationMembersQuery, GetOrganizationMembersResponse> handler)
    {
        var result = await handler.HandleAsync(new GetOrganizationMembersQuery(id));
        return Ok(result);
    }

    [Authorize(Policy = "PlatformAdminOnly")]
    [HttpDelete("{id:guid}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        string userId,
        [FromServices] ICommandHandler<RemoveOrganizationMemberCommand, RemoveOrganizationMemberResponse> handler)
    {
        var result = await handler.HandleAsync(new RemoveOrganizationMemberCommand(id, userId));
        if (!result.Success) return NotFound(result);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyOrganizations(
        [FromServices] IQueryHandler<GetMyOrganizationsQuery, GetMyOrganizationsResponse> handler)
    {
        var userId = User.FindFirstValue(ClaimNames.UserId)
            ?? throw new UnauthorizedAccessException("userId claim not found");
        var result = await handler.HandleAsync(new GetMyOrganizationsQuery(userId));
        return Ok(result);
    }
}

public record CreateOrganizationRequest(string Name, string Slug, string? Description);
public record UpdateOrganizationRequest(string Name, string? Description);
public record AddMemberRequest(string UserId, string Role);
