using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 🏷️ The Category Service Counter 🏷️
    /// Retrieves service counts for each category.
    /// </summary>
    public class GetServiceCountByCategory
    {
        private readonly ILogger<GetServiceCountByCategory> _logger;
        private readonly ProjectContext _context;
        
        public GetServiceCountByCategory(ILogger<GetServiceCountByCategory> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🏷️ The Magical Service Count Retrieval Ritual 🏷️
        /// Azure Function triggered by HTTP GET to retrieve service counts by category.
        /// </summary>
        [Function("GetServiceCountByCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "categories/service-counts")] HttpRequest req)
        {
            _logger.LogInformation("🏷️ Service count by category request received.");

            // Handle CORS
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (req.Method == "OPTIONS")
            {
                return new OkResult();
            }

            try
            {
                _logger.LogInformation("🔍 Retrieving service counts by category from database");
                
                var categoryCounts = await _context.Categories
                    .Select(c => new
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        ServiceCount = c.Services.Count()
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
                    
                _logger.LogInformation("✅ Retrieved service counts for {Count} categories", categoryCounts.Count);
                
                // Ensure headers are present on response
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                
                return new OkObjectResult(categoryCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving service counts by category!");
                return new ObjectResult("An unexpected error occurred while retrieving service counts.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
