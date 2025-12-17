using Serilog.Context;

namespace ProjectManager.Middleware
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Check if the header already exists (e.g., from a frontend or load balancer), otherwise generate one
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() ?? Guid.NewGuid().ToString();

            // 2. Add to Response Headers (so Frontend sees it)
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            // 3. Push to Serilog Context
            // This ensures every log written during this request has this ID attached automatically
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}