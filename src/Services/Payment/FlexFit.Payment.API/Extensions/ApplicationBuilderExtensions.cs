using FlexFit.Payment.API.Middleware;
using Microsoft.AspNetCore.Builder;

namespace FlexFit.Payment.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePaymentExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}


