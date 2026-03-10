using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;

using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace API.Functions
{
    /// <summary>
    /// 💅 The Service Menu Master 💅
    /// Retrieves all available services with their categories.
    /// </summary>
    public class GetServiceList
    {        private readonly ILogger<GetServiceList> _logger;
        private readonly ProjectContext _context;
        
        public GetServiceList(ILogger<GetServiceList> logger, ProjectContext context)        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        
        /// <summary>
        /// 💅 The Magical Service Menu Ritual 💅
        /// Azure Function triggered by HTTP GET to retrieve all available services.
        /// </summary>
        [Function("GetServiceList")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "services")] HttpRequest req)
        {
            _logger.LogInformation("💅 Service list request received.");

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
                _logger.LogInformation("🔍 Retrieving all services with categories from database");
                  var services = await _context.Services
                    .Include(s => s.Category)
                    .AsNoTracking()
                    .OrderBy(s => s.Category.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.AfterCareInstructions,
                        s.Price,
                        s.Duration,
                        s.AppointmentBufferTime,
                        s.Website,
                        Category = new
                        {
                            s.Category.Id,
                            s.Category.Name
                        }
                    })
                    .ToListAsync();
                    
                _logger.LogInformation("✅ Retrieved {Count} services from database", services.Count);
                
                // Ensure headers are present on response
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving services!");
                return new ObjectResult("An unexpected error occurred while retrieving services.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
