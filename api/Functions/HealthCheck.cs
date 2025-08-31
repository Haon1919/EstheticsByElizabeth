using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 🏥 Health Check Function 🏥
    /// Simple health check to verify the API is running
    /// </summary>
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;

        public HealthCheck(ILogger<HealthCheck> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 🏥 API Health Check
        /// Returns a simple status to verify the API is running
        /// </summary>
        [Function("HealthCheck")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
        {
            _logger.LogInformation("🏥 Health check request received");

            try
            {
                var response = new
                {
                    status = "healthy",
                    timestamp = DateTimeOffset.UtcNow,
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "unknown"
                };

                _logger.LogInformation("✅ Health check completed successfully");
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Health check failed");
                return new ObjectResult(new { status = "unhealthy", error = ex.Message })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
