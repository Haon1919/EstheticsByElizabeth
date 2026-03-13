using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using API.Data;
using API.Services;
using System;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 🏥 Detailed Health Check Function 🏥
    /// Validates dependencies like database and storage
    /// </summary>
    public class HealthCheck
    {
        private readonly ILogger<HealthCheck> _logger;
        private readonly ProjectContext _context;
        private readonly IImageStorageService _storageService;

        public HealthCheck(ILogger<HealthCheck> logger, ProjectContext context, IImageStorageService storageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// 🏥 API Health Check
        /// Returns a detailed status of the API and its dependencies
        /// </summary>
        [Function("HealthCheck")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("🏥 Health check request received");

            bool dbHealthy = false;
            bool storageHealthy = false;

            try
            {
                dbHealthy = await _context.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
            }

            try
            {
                // Simple check to see if storage is accessible
                await _storageService.ImageExistsAsync("healthcheck-dummy-file");
                storageHealthy = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Storage health check failed");
            }

            string overallStatus = (dbHealthy && storageHealthy) ? "healthy" : (dbHealthy || storageHealthy) ? "degraded" : "unhealthy";
            var statusCode = overallStatus == "healthy" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;

            var healthStatus = new
            {
                status = overallStatus,
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "unknown",
                runtime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") ?? "unknown",
                dependencies = new
                {
                    database = dbHealthy ? "healthy" : "unhealthy",
                    storage = storageHealthy ? "healthy" : "unhealthy"
                }
            };

            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            
            await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
            
            if (overallStatus == "healthy")
            {
                _logger.LogInformation("✅ Health check completed successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ Health check completed with status: {Status}", overallStatus);
            }

            return response;
        }
    }
}