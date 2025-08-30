using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using API.Services;
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
    /// 💅 The Service Updater 💅
    /// Updates existing services for the beauty business.
    /// </summary>
    public class UpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly ProjectContext _context;
        
        public UpdateService(ILogger<UpdateService> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 💅 The Magical Service Update Ritual 💅
        /// Azure Function triggered by HTTP PUT to update an existing service.
        /// </summary>
        [Function("UpdateService")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/services/{serviceId:int}")] HttpRequest req,
            int serviceId)
        {
            _logger.LogInformation("💅 Update service request received for service ID: {ServiceId}", serviceId);

            if (!AuthTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }
            
            try
            {
                // Check if service exists
                var existingService = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == serviceId);
                    
                if (existingService == null)
                {
                    return new NotFoundObjectResult($"Service with ID {serviceId} not found.");
                }

                // Read the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    return new BadRequestObjectResult("Request body cannot be empty.");
                }

                var updateRequest = JsonSerializer.Deserialize<UpdateServiceRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (updateRequest == null)
                {
                    return new BadRequestObjectResult("Invalid request format.");
                }

                // Validate the request
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(updateRequest);
                
                if (!Validator.TryValidateObject(updateRequest, validationContext, validationResults, true))
                {
                    var errors = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
                    return new BadRequestObjectResult($"Validation failed: {errors}");
                }

                // If category is being changed, check if it exists
                if (updateRequest.CategoryId.HasValue && updateRequest.CategoryId != existingService.CategoryId)
                {
                    var categoryExists = await _context.Categories
                        .AnyAsync(c => c.Id == updateRequest.CategoryId.Value);
                        
                    if (!categoryExists)
                    {
                        return new BadRequestObjectResult("Category not found.");
                    }
                }

                // Check if service name already exists in the category (excluding current service)
                if (!string.IsNullOrEmpty(updateRequest.Name))
                {
                    var categoryId = updateRequest.CategoryId ?? existingService.CategoryId;
                    var nameExists = await _context.Services
                        .AnyAsync(s => s.Name.ToLower() == updateRequest.Name.ToLower() && 
                                      s.CategoryId == categoryId &&
                                      s.Id != serviceId);
                                      
                    if (nameExists)
                    {
                        return new BadRequestObjectResult("A service with this name already exists in the selected category.");
                    }
                }

                _logger.LogInformation("🔧 Updating service: {ServiceName}", existingService.Name);
                
                // Update the service properties
                if (!string.IsNullOrEmpty(updateRequest.Name))
                    existingService.Name = updateRequest.Name;
                    
                if (updateRequest.Description != null)
                    existingService.Description = updateRequest.Description;
                    
                if (updateRequest.AfterCareInstructions != null)
                    existingService.AfterCareInstructions = updateRequest.AfterCareInstructions;
                    
                if (updateRequest.Price.HasValue)
                    existingService.Price = updateRequest.Price.Value;
                    
                if (updateRequest.Duration.HasValue)
                    existingService.Duration = updateRequest.Duration.Value;
                    
                if (updateRequest.AppointmentBufferTime.HasValue)
                    existingService.AppointmentBufferTime = updateRequest.AppointmentBufferTime.Value;
                    
                if (updateRequest.CategoryId.HasValue)
                    existingService.CategoryId = updateRequest.CategoryId.Value;
                    
                if (updateRequest.Website != null)
                    existingService.Website = updateRequest.Website;

                await _context.SaveChangesAsync();

                // Fetch the updated service with category info
                var updatedService = await _context.Services
                    .Include(s => s.Category)
                    .FirstAsync(s => s.Id == serviceId);

                var response = new
                {
                    updatedService.Id,
                    updatedService.Name,
                    updatedService.Description,
                    updatedService.AfterCareInstructions,
                    updatedService.Price,
                    updatedService.Duration,
                    updatedService.AppointmentBufferTime,
                    updatedService.Website,
                    Category = new
                    {
                        updatedService.Category.Id,
                        updatedService.Category.Name
                    }
                };

                _logger.LogInformation("✅ Service updated successfully: {ServiceId}", serviceId);
                
                return new OkObjectResult(response);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 Invalid JSON in request body!");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while updating service!");
                return new ObjectResult("An unexpected error occurred while updating the service.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    public class UpdateServiceRequest
    {
        [StringLength(255, ErrorMessage = "Service name cannot exceed 255 characters")]
        public string? Name { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [StringLength(2000, ErrorMessage = "Aftercare instructions cannot exceed 2000 characters")]
        public string? AfterCareInstructions { get; set; }

        [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99")]
        public decimal? Price { get; set; }

        [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes")]
        public int? Duration { get; set; }

        [Range(1, 52, ErrorMessage = "Appointment buffer time must be between 1 and 52 weeks")]
        public int? AppointmentBufferTime { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(2048, ErrorMessage = "Website URL cannot exceed 2048 characters")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? Website { get; set; }
    }
}
