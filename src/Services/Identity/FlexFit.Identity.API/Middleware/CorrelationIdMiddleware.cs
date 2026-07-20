using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FlexFit.Identity.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly Microsoft.Extensions.Logging.ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        Microsoft.Extensions.Logging.ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdHeader) &&
            !string.IsNullOrWhiteSpace(correlationIdHeader))
        {
            correlationId = correlationIdHeader.ToString();
            if (correlationId.Length > 100)
            {
                correlationId = correlationId.Substring(0, 100);
            }
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // Set on response
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Set on HttpContext.Items for general retrieval
        context.Items[CorrelationIdHeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
