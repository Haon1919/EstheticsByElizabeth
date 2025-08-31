using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace API.Functions
{
    /// <summary>
    /// 🏥 Simple Health Check Function 🏥
    /// Basic health check that doesn't depend on external services
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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("🏥 Health check request received");

            try
            {
                var healthStatus = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "unknown",
                    runtime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") ?? "unknown"
                };

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                
                await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
                
                _logger.LogInformation("✅ Health check completed successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Health check failed");
                
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                
                var errorResponse = new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                };
                
                await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
                return response;
            }
        }
    }
}
