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
    /// 🏷️ The Category Curator 🏷️
    /// Retrieves all available service categories.
    /// </summary>
    public class GetCategories
    {
        private readonly ILogger<GetCategories> _logger;
        private readonly ProjectContext _context;
        
        public GetCategories(ILogger<GetCategories> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }        /// <summary>
        /// 🏷️ The Magical Category Retrieval Ritual 🏷️
        /// Azure Function triggered by HTTP GET to retrieve all service categories.
        /// </summary>
        [Function("GetCategories")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "categories")] HttpRequest req)
        {
            _logger.LogInformation("🏷️ Service categories request received.");

            // Handle CORS
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, OPTIONS");
            req.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (req.Method == "OPTIONS")
            {
                return new OkResult();
            }

            try
            {
                _logger.LogInformation("🔍 Retrieving all service categories from database");
                
                var categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                    
                _logger.LogInformation("✅ Retrieved {Count} categories from database", categories.Count);
                
                // Ensure headers are present on response
                
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
