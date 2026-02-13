using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PetAdoption.PetService.Application.DTOs;
using PetAdoption.PetService.Domain.Exceptions;

namespace PetAdoption.PetService.Infrastructure.Middleware;

/// <summary>
/// Middleware to catch domain exceptions and transform them into standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    private async Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        _logger.LogWarning(exception,
            "Domain exception occurred: {ErrorCode} - {Message}",
            exception.ErrorCode,
            exception.Message);

        var statusCode = MapErrorCodeToHttpStatus(exception.ErrorCode);

        var errorResponse = new ErrorResponse(
            exception.ErrorCode,
            exception.Message,
            exception.Metadata as IDictionary<string, object>);

        await WriteErrorResponseAsync(context, statusCode, errorResponse);
    }

    private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception,
            "Unexpected error occurred: {Message}",
            exception.Message);

        var errorResponse = new ErrorResponse(
            errorCode: "InternalServerError",
            errorCodeValue: 5000,
            message: "An unexpected error occurred. Please try again later.",
            details: new Dictionary<string, object>
            {
                { "ExceptionType", exception.GetType().Name }
            });

        await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, errorResponse);
    }

    private static HttpStatusCode MapErrorCodeToHttpStatus(PetDomainErrorCode errorCode)
    {
        return errorCode switch
        {
            // Not found errors
            PetDomainErrorCode.PetNotFound => HttpStatusCode.NotFound,

            // Validation errors
            PetDomainErrorCode.InvalidPetName => HttpStatusCode.BadRequest,
            PetDomainErrorCode.InvalidPetType => HttpStatusCode.BadRequest,

            // Business rule violations
            PetDomainErrorCode.PetNotAvailable => HttpStatusCode.Conflict,
            PetDomainErrorCode.PetNotReserved => HttpStatusCode.Conflict,
            PetDomainErrorCode.ConcurrencyConflict => HttpStatusCode.Conflict,
            PetDomainErrorCode.InvalidOperation => HttpStatusCode.Conflict,

            // Unknown/default
            _ => HttpStatusCode.InternalServerError
        };
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        ErrorResponse errorResponse)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }
}
