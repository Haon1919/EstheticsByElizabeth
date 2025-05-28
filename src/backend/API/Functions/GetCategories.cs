using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API
{
    public class GetCategories
    {
        private readonly ILogger<GetCategories> _logger;
        private readonly ProjectContext _context;

        public GetCategories(ILogger<GetCategories> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetCategories")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("GetCategories function processing request.");
            
            try 
            {
                var categories = await _context.Categories.ToListAsync();
                _logger.LogInformation($"Retrieved {categories.Count} categories from database.");
                return new OkObjectResult(categories);
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error retrieving categories: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
