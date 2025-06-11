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
    /// 💅 The Service Creator 💅
    /// Creates new services for the beauty business.
    /// </summary>
    public class CreateService
    {
        private readonly ILogger<CreateService> _logger;
        private readonly ProjectContext _context;
        
        public CreateService(ILogger<CreateService> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 💅 The Magical Service Creation Ritual 💅
        /// Azure Function triggered by HTTP POST to create a new service.
        /// </summary>
        [Function("CreateService")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/services")] HttpRequest req)
        {
            _logger.LogInformation("💅 Create service request received.");
            
            try
            {
                // Read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult("Request body cannot be empty.");
                }

                var serviceRequest = JsonSerializer.Deserialize<CreateServiceRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (serviceRequest == null)
                {
                    return new BadRequestObjectResult("Invalid request format.");
                }

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(serviceRequest);
                
                if (!Validator.TryValidateObject(serviceRequest, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
                    return new BadRequestObjectResult($"Validation failed: {errors}");
                }

                // Check if category exists
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == serviceRequest.CategoryId);
                    
                if (!categoryExists)
                {
                    return new BadRequestObjectResult("Category not found.");
                }

                // Check if service name already exists in the category
                var existingService = await _context.Services
                    .AnyAsync(s => s.Name.ToLower() == serviceRequest.Name.ToLower() && 
                                  s.CategoryId == serviceRequest.CategoryId);
                                  
                if (existingService)
                {
                    return new BadRequestObjectResult("A service with this name already exists in the selected category.");
                }

                _logger.LogInformation("🔨 Creating new service: {ServiceName}", serviceRequest.Name);
                
                // Create the service
                var service = new Service
                {
                    Name = serviceRequest.Name,
                    Description = serviceRequest.Description,
                    Price = serviceRequest.Price,
                    Duration = serviceRequest.Duration,
                    CategoryId = serviceRequest.CategoryId,
                    Website = serviceRequest.Website
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                // Fetch the created service with category info
                var createdService = await _context.Services
                    .Include(s => s.Category)
                    .FirstAsync(s => s.Id == service.Id);

                var response = new
                {
                    createdService.Id,
                    createdService.Name,
                    createdService.Description,
                    createdService.Price,
                    createdService.Duration,
                    createdService.Website,
                    Category = new
                    {
                        createdService.Category.Id,
                        createdService.Category.Name
                    }
                };

                _logger.LogInformation("✅ Service created successfully with ID: {ServiceId}", service.Id);
                
                return new CreatedResult($"/services/{service.Id}", response);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 Invalid JSON in request body!");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while creating service!");
                return new ObjectResult("An unexpected error occurred while creating the service.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    public class CreateServiceRequest
    {
        [Required(ErrorMessage = "Service name is required")]
        [StringLength(255, ErrorMessage = "Service name cannot exceed 255 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99")]
        public decimal? Price { get; set; }

        [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes")]
        public int? Duration { get; set; }

        [Required(ErrorMessage = "Category ID is required")]
        public int CategoryId { get; set; }

        [StringLength(2048, ErrorMessage = "Website URL cannot exceed 2048 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? Website { get; set; }
    }
}
