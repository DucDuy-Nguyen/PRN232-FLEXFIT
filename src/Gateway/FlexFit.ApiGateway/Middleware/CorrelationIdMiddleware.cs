using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlexFit.ApiGateway.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
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
            // Sanitize and limit correlation ID
            correlationId = Regex.Replace(correlationId, @"[^a-zA-Z0-9\-_]", "");
            if (correlationId.Length > 128)
            {
                correlationId = correlationId.Substring(0, 128);
            }
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // Set response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Set request header to ensure YARP forwards it downstream
        context.Request.Headers[CorrelationIdHeaderName] = correlationId;

        // Push correlation context into Microsoft logging
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
