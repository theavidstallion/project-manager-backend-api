using System.Net;
using System.Text.Json;

namespace ProjectManager.Middleware
{
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
                await _next(context); // Try to run the request
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex); // Catch crash
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // 1. Get the Correlation ID we set earlier
            var correlationId = context.Response.Headers["X-Correlation-ID"].ToString();

            // 2. Log the actual error internally (with stack trace)
            _logger.LogError(ex, "An unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

            // 3. Prepare clean response for User
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error. Please contact support.",
                CorrelationId = correlationId // User gives this ID to Admin for support
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}