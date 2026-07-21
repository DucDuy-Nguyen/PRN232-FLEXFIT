using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlexFit.Payment.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            var statusCode = exception switch
            {
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException ioe when ioe.Message.Contains("PayOS") => HttpStatusCode.ServiceUnavailable,
                InvalidOperationException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;

            var title = exception switch
            {
                ArgumentException => "Yêu cầu không hợp lệ",
                UnauthorizedAccessException => "Không có quyền truy cập",
                KeyNotFoundException => "Không tìm thấy tài nguyên",
                InvalidOperationException ioe when ioe.Message.Contains("PayOS") => "Dịch vụ thanh toán không khả dụng",
                InvalidOperationException => "Xung đột dữ liệu hoặc lỗi vận hành",
                _ => "Lỗi hệ thống nội bộ"
            };

            var detail = exception.Message;

            var problemDetails = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{(int)statusCode}",
                Title = title,
                Status = (int)statusCode,
                Detail = detail,
                Instance = context.Request.Path,
            };

            // Set traceId from Activity or TraceIdentifier
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            if (_env.IsDevelopment())
            {
                problemDetails.Extensions["exception"] = exception.ToString();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(problemDetails, options);
            await context.Response.WriteAsync(json);
        }
    }
}


