using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace API.Middleware
{
    /// <summary>
    /// CORS middleware for Azure Functions Worker to handle cross-origin requests
    /// </summary>
    public class CorsMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<CorsMiddleware> _logger;

        public CorsMiddleware(ILogger<CorsMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext != null)
            {
                // Add CORS headers to all responses
                httpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
                httpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                httpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, Origin, Accept, X-Requested-With";
                httpContext.Response.Headers["Access-Control-Max-Age"] = "86400";

                // Handle preflight OPTIONS requests
                if (httpContext.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("🔄 CORS middleware handling OPTIONS preflight request");
                    httpContext.Response.StatusCode = 200;
                    return; // Don't call next() for OPTIONS requests
                }
            }

            // Continue to the next middleware/function
            await next(context);
        }
    }
}
