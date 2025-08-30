using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using API.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 🏷️ The Category Eliminator 🏷️
    /// Deletes service categories with safety checks.
    /// </summary>
    public class DeleteCategory
    {
        private readonly ILogger<DeleteCategory> _logger;
        private readonly ProjectContext _context;
        
        public DeleteCategory(ILogger<DeleteCategory> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🏷️ The Magical Category Deletion Ritual 🏷️
        /// Azure Function triggered by HTTP DELETE to remove a category.
        /// Safety check: Cannot delete categories with existing services.
        /// </summary>
        [Function("DeleteCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/categories/{categoryId:int}")] HttpRequest req,
            int categoryId)
        {
            _logger.LogInformation("🏷️ Delete category request received for ID {CategoryId}.", categoryId);

            if (!AuthTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }
            
            try
            {
                // Check if category exists
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return new NotFoundObjectResult($"Category with ID {categoryId} not found.");
                }

                // Safety check: Ensure category has no services
                var serviceCount = await _context.Services
                    .Where(s => s.CategoryId == categoryId)
                    .CountAsync();

                if (serviceCount > 0)
                {
                    return new BadRequestObjectResult($"Cannot delete category '{category.Name}' because it has {serviceCount} service(s) associated with it. Please delete or reassign the services first.");
                }

                // Delete the category
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Category '{CategoryName}' deleted successfully with ID {CategoryId}", category.Name, categoryId);

                return new OkObjectResult(new { message = $"Category '{category.Name}' deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while deleting category!");
                return new ObjectResult("An unexpected error occurred while deleting the category.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
