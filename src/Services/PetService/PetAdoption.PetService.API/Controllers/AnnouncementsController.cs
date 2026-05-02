using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.PetService.Application.Abstractions;
using PetAdoption.PetService.Application.Commands;
using PetAdoption.PetService.Application.Queries;

namespace PetAdoption.PetService.API.Controllers;

[ApiController]
[Route("api/announcements")]
public class AnnouncementsController : PetServiceControllerBase
{
    private readonly IMediator _mediator;

    public AnnouncementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest request)
    {
        var result = await _mediator.Send(new CreateAnnouncementCommand(
            request.Title, request.Body, request.StartDate, request.EndDate, GetUserId()));
        return StatusCode(201, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnnouncementRequest request)
    {
        var result = await _mediator.Send(new UpdateAnnouncementCommand(
            id, request.Title, request.Body, request.StartDate, request.EndDate));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAnnouncementCommand(id));
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10)
    {
        take = Math.Min(take, 100);
        var result = await _mediator.Send(new GetAnnouncementsQuery(skip, take));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetAnnouncementByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveAnnouncementsQuery());
        return Ok(result);
    }
}

public record CreateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
public record UpdateAnnouncementRequest(string Title, string Body, DateTime StartDate, DateTime EndDate);
