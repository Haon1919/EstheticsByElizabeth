using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
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
    /// 🏷️ The Category Creator 🏷️
    /// Creates new service categories for the beauty business.
    /// </summary>
    public class CreateCategory
    {
        private readonly ILogger<CreateCategory> _logger;
        private readonly ProjectContext _context;
        
        public CreateCategory(ILogger<CreateCategory> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🏷️ The Magical Category Creation Ritual 🏷️
        /// Azure Function triggered by HTTP POST to create a new category.
        /// </summary>
        [Function("CreateCategory")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/categories")] HttpRequest req)
        {
            _logger.LogInformation("🏷️ Create category request received.");
            
            try
            {
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
                
                var categoryRequest = JsonSerializer.Deserialize<CreateCategoryRequest>(requestBody, options);
                
                if (categoryRequest == null)
                {
                    return new BadRequestObjectResult("Invalid JSON format.");
                }

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(categoryRequest);
                bool isValid = Validator.TryValidateObject(categoryRequest, validationContext, validationResults, true);

                if (!isValid)
                {
                    var errors = validationResults.Select(vr => vr.ErrorMessage).ToArray();
                    return new BadRequestObjectResult($"Validation failed: {string.Join(", ", errors)}");
                }

                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == categoryRequest.Name.ToLower());

                if (existingCategory != null)
                {
                    return new BadRequestObjectResult("A category with this name already exists.");
                }

                // Create new category
                var category = new Category
                {
                    Name = categoryRequest.Name.Trim()
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Category created successfully with ID {CategoryId}", category.Id);

                return new CreatedResult($"/api/categories/{category.Id}", category);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Invalid JSON in request body");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while creating category!");
                return new ObjectResult("An unexpected error occurred while creating the category.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    // Request DTO for creating categories
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Category name must be between 1 and 255 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
