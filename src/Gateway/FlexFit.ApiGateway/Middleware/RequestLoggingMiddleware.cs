using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlexFit.ApiGateway.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
            stopwatch.Stop();

            LogRequest(context, stopwatch.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogRequest(context, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    private void LogRequest(HttpContext context, long elapsedMilliseconds, Exception? exception)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value;
        var statusCode = context.Response.StatusCode;

        // Retrieve Correlation ID
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader)
            ? correlationHeader.ToString()
            : context.TraceIdentifier;

        // Safely extract authenticated user ID if exists
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? context.User?.FindFirst("sub")?.Value 
                     ?? "Anonymous";

        if (exception != null)
        {
            _logger.LogError(
                exception,
                "API Gateway Request Failed | Method: {Method} | Path: {Path} | Status: {Status} | Time: {Elapsed}ms | CorrelationId: {CorrelationId} | User: {User}",
                method, path, statusCode, elapsedMilliseconds, correlationId, userId);
        }
        else
        {
            _logger.LogInformation(
                "API Gateway Request Completed | Method: {Method} | Path: {Path} | Status: {Status} | Time: {Elapsed}ms | CorrelationId: {CorrelationId} | User: {User}",
                method, path, statusCode, elapsedMilliseconds, correlationId, userId);
        }
    }
}
