using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API
{
    public class GetServiceList
    {
        private readonly ILogger<GetServiceList> _logger;
        private readonly ProjectContext _context;

        public GetServiceList(ILogger<GetServiceList> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetServiceList")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("GetServiceList function processing request.");
            
            try 
            {
                var services = await _context.Services
                    .Include(s => s.Category)
                    .ToListAsync();
                    
                _logger.LogInformation($"Retrieved {services.Count} services from database.");
                return new OkObjectResult(services);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error retrieving services: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
