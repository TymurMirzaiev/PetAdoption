using System.Net;
using System.Text.Json;
using PetAdoption.UserService.Domain.Exceptions;

namespace PetAdoption.UserService.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Code} - {Message}", ex.Code, ex.Message);
            await WriteResponseAsync(context, MapDomainException(ex), ex.Code, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, "validation_error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {Message}", ex.Message);
            await WriteResponseAsync(context, HttpStatusCode.InternalServerError, "internal_error", "An unexpected error occurred.");
        }
    }

    private static HttpStatusCode MapDomainException(DomainException ex) => ex switch
    {
        DuplicateEmailException => HttpStatusCode.Conflict,
        InvalidCredentialsException => HttpStatusCode.Unauthorized,
        UserNotFoundException => HttpStatusCode.NotFound,
        UserSuspendedException => HttpStatusCode.Forbidden,
        _ => HttpStatusCode.BadRequest
    };

    private static async Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { ErrorCode = code, Message = message };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
