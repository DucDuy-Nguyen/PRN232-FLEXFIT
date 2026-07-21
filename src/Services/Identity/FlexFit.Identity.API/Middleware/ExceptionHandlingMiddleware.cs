using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlexFit.Identity.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (status, title, detail, errors) = MapException(exception);

        context.Response.StatusCode = status;

        var traceId = context.Items.TryGetValue("X-Correlation-ID", out var correlationId)
            ? correlationId?.ToString()
            : context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = GetProblemType(status)
        };

        problemDetails.Extensions["traceId"] = traceId;

        if (errors != null && errors.Any())
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static (int Status, string Title, string Detail, Dictionary<string, string[]>? Errors) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),
            
            UnauthorizedAccessException uaEx => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                string.IsNullOrWhiteSpace(uaEx.Message) ? "Authentication is required to access this resource." : uaEx.Message,
                null
            ),

            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                null
            ),

            InvalidOperationException opEx when opEx.Message.Contains("lock", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("cooldown", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("too many", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status429TooManyRequests,
                "Too many requests",
                opEx.Message,
                null
            ),

            InvalidOperationException opEx when opEx.Message.Contains("deactivated", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("suspended", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status403Forbidden,
                "Account not accessible",
                opEx.Message,
                null
            ),

            InvalidOperationException opEx when opEx.Message.Contains("cannot revoke", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("last administrator", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("already has the", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("already", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                "Conflict",
                opEx.Message,
                null
            ),

            InvalidOperationException opEx when opEx.Message.Contains("invalid email or password", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("incorrect current password", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("credentials", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                opEx.Message,
                null
            ),

            InvalidOperationException opEx when opEx.Message.Contains("exists", StringComparison.OrdinalIgnoreCase) ||
                                                opEx.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status409Conflict,
                "Conflict",
                opEx.Message,
                null
            ),

            InvalidOperationException opEx => (
                StatusCodes.Status400BadRequest,
                "Bad request",
                opEx.Message,
                null
            ),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred on the server.",
                null
            )
        };
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
            403 => "https://tools.ietf.org/html/rfc7235#section-3.2",
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
