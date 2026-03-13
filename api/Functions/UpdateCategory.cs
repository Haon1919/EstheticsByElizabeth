using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using API.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace API.Functions
{
    /// <summary>
    /// 🏷️ The Category Updater 🏷️
    /// Updates existing service categories.
    /// </summary>
    public class UpdateCategory
    {
        private readonly IAuthTokenService _authTokenService;
        private readonly ILogger<UpdateCategory> _logger;
        private readonly ProjectContext _context;
        
        public UpdateCategory(ILogger<UpdateCategory> logger, ProjectContext context, IAuthTokenService authTokenService)
        {
            _authTokenService = authTokenService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🏷️ The Magical Category Update Ritual 🏷️
        /// Azure Function triggered by HTTP PUT to update an existing category.
        /// </summary>
        [Function("UpdateCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/categories/{categoryId:int}")] HttpRequest req,
            int categoryId)
        {
            _logger.LogInformation("🏷️ Update category request received for ID {CategoryId}.", categoryId);

            if (!_authTokenService.ValidateRequest(req))
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

                // Read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult("Request body cannot be empty.");
                }

                // Parse the request JSON
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var updateRequest = JsonSerializer.Deserialize<UpdateCategoryRequest>(requestBody, options);
                
                if (updateRequest == null)
                {
                    return new BadRequestObjectResult("Invalid JSON format.");
                }

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(updateRequest);
                bool isValid = Validator.TryValidateObject(updateRequest, validationContext, validationResults, true);

                if (!isValid)
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                    return new BadRequestObjectResult($"Validation failed: {string.Join(", ", errors)}");
                }

                // Check if category name already exists (excluding current category)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == updateRequest.Name.ToLower() && c.Id != categoryId);

                if (existingCategory != null)
                {
                    return new BadRequestObjectResult("A category with this name already exists.");
                }

                // Update category
                category.Name = updateRequest.Name.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Category updated successfully with ID {CategoryId}", category.Id);

                return new OkObjectResult(category);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Invalid JSON in request body");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while updating category!");
                return new ObjectResult("An unexpected error occurred while updating the category.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    // Request DTO for updating categories
    public class UpdateCategoryRequest
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
