using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 🏷️ The Category Curator 🏷️
    /// Retrieves all available service categories.
    /// </summary>
    public class GetCategories
    {
        private readonly ILogger<GetCategories> _logger;
        private readonly ProjectContext _context;        public GetCategories(ILogger<GetCategories> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🏷️ The Magical Category Retrieval Ritual 🏷️
        /// Azure Function triggered by HTTP GET to retrieve all service categories.
        /// </summary>
        [Function("GetCategories")]
        [Cors]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "categories")] HttpRequest req)
        {
            _logger.LogInformation("🏷️ Service categories request received.");
              // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                req.HttpContext.Response.Headers.Add("Access-Control-Max-Age", "86400");
                
                return response;
            }
              // Add CORS headers to all responses
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
              try
            {
                _logger.LogInformation("🔍 Retrieving all service categories from database");
                
                var categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                    
                _logger.LogInformation("✅ Retrieved {Count} categories from database", categories.Count);
                
                return new OkObjectResult(categories);
            }
            // Catch any unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving categories!");
                return new ObjectResult("An unexpected error occurred while retrieving categories.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
