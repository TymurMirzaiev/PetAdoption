namespace PetAdoption.UserService.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Application.Queries;
using PetAdoption.UserService.Domain.ValueObjects;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    // PUBLIC ENDPOINTS (No authentication required)

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] ICommandHandler<RegisterUserCommand, RegisterUserResponse> handler)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.FullName,
            request.Password,
            request.PhoneNumber
        );

        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Login (sign in) with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ICommandHandler<LoginCommand, LoginResponse> handler)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    // AUTHENTICATED ENDPOINTS (Require valid JWT token)

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile(
        [FromServices] IQueryHandler<GetUserByIdQuery, UserDto> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var query = new GetUserByIdQuery(userId);
        var user = await handler.HandleAsync(query);

        return Ok(user);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResponse> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var command = new UpdateUserProfileCommand(
            userId,
            request.FullName,
            request.PhoneNumber,
            request.Preferences
        );

        var response = await handler.HandleAsync(command);
        return Ok(response);
    }

    /// <summary>
    /// Change current user's password
    /// </summary>
    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler)
    {
        var userId = User.FindFirstValue("userId")
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        var command = new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );

        var response = await handler.HandleAsync(command);
        return Ok(response);
    }

    // ADMIN-ONLY ENDPOINTS

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserById(
        string id,
        [FromServices] IQueryHandler<GetUserByIdQuery, UserDto> handler)
    {
        var query = new GetUserByIdQuery(id);
        var user = await handler.HandleAsync(query);

        return Ok(user);
    }

    /// <summary>
    /// List all users (Admin only, paginated)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUsers(
        [FromServices] IQueryHandler<GetUsersQuery, GetUsersResponse> handler,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetUsersQuery(skip, take);
        var response = await handler.HandleAsync(query);

        return Ok(response);
    }

    /// <summary>
    /// Suspend a user (Admin only)
    /// </summary>
    [HttpPost("{id}/suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SuspendUser(
        string id,
        [FromBody] SuspendUserRequest request,
        [FromServices] ICommandHandler<SuspendUserCommand, SuspendUserResponse> handler)
    {
        var command = new SuspendUserCommand(id, request.Reason);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }

    /// <summary>
    /// Promote user to admin (Admin only)
    /// </summary>
    [HttpPost("{id}/promote-to-admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PromoteToAdmin(
        string id,
        [FromServices] ICommandHandler<PromoteToAdminCommand, PromoteToAdminResponse> handler)
    {
        var command = new PromoteToAdminCommand(id);
        var response = await handler.HandleAsync(command);

        return Ok(response);
    }
}

// Request DTOs
public record RegisterUserRequest(
    string Email,
    string FullName,
    string Password,
    string? PhoneNumber = null
);

public record LoginRequest(string Email, string Password);

public record UpdateProfileRequest(
    string? FullName,
    string? PhoneNumber,
    UserPreferences? Preferences
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record SuspendUserRequest(string Reason);
