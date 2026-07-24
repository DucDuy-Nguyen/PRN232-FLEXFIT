using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlexFit.ApiGateway.Middleware;

public sealed class GatewayExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayExceptionMiddleware> _logger;

    public GatewayExceptionMiddleware(RequestDelegate next, ILogger<GatewayExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred in the API Gateway.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var status = StatusCodes.Status500InternalServerError;
        var title = "Gateway Error";
        var detail = "An error occurred inside the API Gateway while processing your request.";

        // Map specific exceptions
        if (exception is TimeoutException)
        {
            status = StatusCodes.Status504GatewayTimeout;
            title = "Gateway Timeout";
            detail = "The downstream service timed out.";
        }

        context.Response.StatusCode = status;

        var traceId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader)
            ? correlationHeader.ToString()
            : context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{status}"
        };

        problemDetails.Extensions["traceId"] = traceId;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
