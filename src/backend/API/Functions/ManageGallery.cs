using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Data;
using API.Entities;
using API.DTOs;

namespace API.Functions
{
    /// <summary>
    /// 🖼️ Gallery Management System 🖼️
    /// Functions for managing gallery images in the admin panel.
    /// </summary>
    public class ManageGallery
    {
        private readonly ILogger<ManageGallery> _logger;
        private readonly ProjectContext _context;

        public ManageGallery(ILogger<ManageGallery> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// 📋 Get all gallery images with optional category filtering
        /// </summary>
        [Function("GetGalleryImages")]
        public async Task<IActionResult> GetGalleryImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/gallery")] HttpRequest req)
        {
            _logger.LogInformation("📋 Gallery images retrieval request received");

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                var categoryFilter = req.Query["category"].ToString();
                
                _logger.LogInformation("🔍 Retrieving gallery images{CategoryFilter}", 
                    !string.IsNullOrEmpty(categoryFilter) ? $" for category: {categoryFilter}" : "");

                var query = _context.GalleryImages.AsQueryable();

                // Apply category filter if provided
                if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "all")
                {
                    query = query.Where(img => img.Category == categoryFilter);
                }

                var images = await query
                    .OrderBy(img => img.SortOrder)
                    .ThenByDescending(img => img.UploadedAt)
                    .Select(img => new GalleryImageResponseDto
                    {
                        Id = img.Id,
                        Src = img.Src,
                        Alt = img.Alt,
                        Category = img.Category,
                        Title = img.Title,
                        Description = img.Description,
                        IsActive = img.IsActive,
                        SortOrder = img.SortOrder,
                        UploadedAt = img.UploadedAt,
                        UpdatedAt = img.UpdatedAt
                    })
                    .ToListAsync();

                // Get all available categories
                var categories = await _context.GalleryImages
                    .Select(img => img.Category)
                    .Distinct()
                    .OrderBy(cat => cat)
                    .ToListAsync();

                var response = new GalleryImageListResponseDto
                {
                    Success = true,
                    Data = images,
                    TotalCount = images.Count,
                    Categories = categories
                };

                _logger.LogInformation("✅ Retrieved {Count} gallery images", images.Count);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error retrieving gallery images");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while retrieving gallery images. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🆕 Create a new gallery image
        /// </summary>
        [Function("CreateGalleryImage")]
        public async Task<IActionResult> CreateGalleryImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/gallery")] HttpRequest req)
        {
            _logger.LogInformation("🆕 Create gallery image request received");

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogWarning("🚫 Empty request body received");
                    return new BadRequestObjectResult("Request body is required.");
                }

                var createDto = JsonSerializer.Deserialize<CreateGalleryImageDto>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (createDto == null)
                {
                    _logger.LogWarning("🚫 Invalid JSON in request body");
                    return new BadRequestObjectResult("Invalid JSON format.");
                }

                _logger.LogInformation("📷 Creating gallery image: {Title} in category: {Category}", 
                    createDto.Title ?? createDto.Alt, createDto.Category);

                var galleryImage = new GalleryImage
                {
                    Src = createDto.Src,
                    Alt = createDto.Alt,
                    Category = createDto.Category,
                    Title = createDto.Title,
                    Description = createDto.Description,
                    IsActive = createDto.IsActive,
                    SortOrder = createDto.SortOrder,
                    UploadedAt = DateTimeOffset.UtcNow
                };

                _context.GalleryImages.Add(galleryImage);
                await _context.SaveChangesAsync();

                var responseDto = new GalleryImageResponseDto
                {
                    Id = galleryImage.Id,
                    Src = galleryImage.Src,
                    Alt = galleryImage.Alt,
                    Category = galleryImage.Category,
                    Title = galleryImage.Title,
                    Description = galleryImage.Description,
                    IsActive = galleryImage.IsActive,
                    SortOrder = galleryImage.SortOrder,
                    UploadedAt = galleryImage.UploadedAt,
                    UpdatedAt = galleryImage.UpdatedAt
                };

                _logger.LogInformation("✅ Gallery image created successfully with ID: {ImageId}", galleryImage.Id);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new CreatedResult($"/api/manage/gallery/{galleryImage.Id}", responseDto);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 JSON parsing error while creating gallery image");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating gallery image");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while creating the gallery image. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 📝 Update an existing gallery image
        /// </summary>
        [Function("UpdateGalleryImage")]
        public async Task<IActionResult> UpdateGalleryImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/gallery/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("📝 Update gallery image request received for ID: {ImageId}", id);

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                if (!int.TryParse(id, out int imageId))
                {
                    _logger.LogWarning("🚫 Invalid image ID format: {Id}", id);
                    return new BadRequestObjectResult("Invalid image ID format.");
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogWarning("🚫 Empty request body received");
                    return new BadRequestObjectResult("Request body is required.");
                }

                var updateDto = JsonSerializer.Deserialize<UpdateGalleryImageDto>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (updateDto == null)
                {
                    _logger.LogWarning("🚫 Invalid JSON in request body");
                    return new BadRequestObjectResult("Invalid JSON format.");
                }

                var existingImage = await _context.GalleryImages.FindAsync(imageId);
                
                if (existingImage == null)
                {
                    _logger.LogWarning("🚫 Gallery image not found with ID: {ImageId}", imageId);
                    return new NotFoundObjectResult($"Gallery image with ID {imageId} not found.");
                }

                _logger.LogInformation("📝 Updating gallery image: {ImageId}", imageId);

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateDto.Src))
                    existingImage.Src = updateDto.Src;
                
                if (!string.IsNullOrEmpty(updateDto.Alt))
                    existingImage.Alt = updateDto.Alt;
                
                if (!string.IsNullOrEmpty(updateDto.Category))
                    existingImage.Category = updateDto.Category;
                
                if (updateDto.Title != null)
                    existingImage.Title = updateDto.Title;
                
                if (updateDto.Description != null)
                    existingImage.Description = updateDto.Description;
                
                if (updateDto.IsActive.HasValue)
                    existingImage.IsActive = updateDto.IsActive.Value;
                
                if (updateDto.SortOrder.HasValue)
                    existingImage.SortOrder = updateDto.SortOrder.Value;

                existingImage.UpdatedAt = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                var responseDto = new GalleryImageResponseDto
                {
                    Id = existingImage.Id,
                    Src = existingImage.Src,
                    Alt = existingImage.Alt,
                    Category = existingImage.Category,
                    Title = existingImage.Title,
                    Description = existingImage.Description,
                    IsActive = existingImage.IsActive,
                    SortOrder = existingImage.SortOrder,
                    UploadedAt = existingImage.UploadedAt,
                    UpdatedAt = existingImage.UpdatedAt
                };

                _logger.LogInformation("✅ Gallery image updated successfully: {ImageId}", imageId);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(responseDto);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 JSON parsing error while updating gallery image");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating gallery image with ID: {ImageId}", id);
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while updating the gallery image. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🗑️ Delete a gallery image
        /// </summary>
        [Function("DeleteGalleryImage")]
        public async Task<IActionResult> DeleteGalleryImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/gallery/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("🗑️ Delete gallery image request received for ID: {ImageId}", id);

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                if (!int.TryParse(id, out int imageId))
                {
                    _logger.LogWarning("🚫 Invalid image ID format: {Id}", id);
                    return new BadRequestObjectResult("Invalid image ID format.");
                }

                var existingImage = await _context.GalleryImages.FindAsync(imageId);
                
                if (existingImage == null)
                {
                    _logger.LogWarning("🚫 Gallery image not found with ID: {ImageId}", imageId);
                    return new NotFoundObjectResult($"Gallery image with ID {imageId} not found.");
                }

                _logger.LogInformation("🗑️ Deleting gallery image: {ImageId} - {Title}", imageId, existingImage.Title ?? existingImage.Alt);

                _context.GalleryImages.Remove(existingImage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Gallery image deleted successfully: {ImageId}", imageId);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(new { success = true, message = "Gallery image deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error deleting gallery image with ID: {ImageId}", id);
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while deleting the gallery image. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🔄 Reorder gallery images
        /// </summary>
        [Function("ReorderGalleryImages")]
        public async Task<IActionResult> ReorderGalleryImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/gallery/reorder")] HttpRequest req)
        {
            _logger.LogInformation("🔄 Reorder gallery images request received");

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogWarning("🚫 Empty request body received");
                    return new BadRequestObjectResult("Request body is required.");
                }

                var reorderDto = JsonSerializer.Deserialize<ReorderImagesDto>(requestBody, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                if (reorderDto?.ImageIds == null || reorderDto.ImageIds.Count == 0)
                {
                    _logger.LogWarning("🚫 No image IDs provided for reordering");
                    return new BadRequestObjectResult("Image IDs are required for reordering.");
                }

                _logger.LogInformation("🔄 Reordering {Count} gallery images", reorderDto.ImageIds.Count);

                // Update sort order for each image
                for (int i = 0; i < reorderDto.ImageIds.Count; i++)
                {
                    var imageId = reorderDto.ImageIds[i];
                    var image = await _context.GalleryImages.FindAsync(imageId);
                    
                    if (image != null)
                    {
                        image.SortOrder = i;
                        image.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Gallery images reordered successfully");

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(new { success = true, message = "Gallery images reordered successfully." });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "💥 JSON parsing error while reordering gallery images");
                return new BadRequestObjectResult("Invalid JSON format in request body.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error reordering gallery images");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while reordering gallery images. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 📊 Get gallery categories with counts
        /// </summary>
        [Function("GetGalleryCategories")]
        public async Task<IActionResult> GetGalleryCategories(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/gallery/categories")] HttpRequest req)
        {
            _logger.LogInformation("📊 Gallery categories request received");

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                var categories = await _context.GalleryImages
                    .GroupBy(img => img.Category)
                    .Select(group => new GalleryCategoryDto
                    {
                        Id = group.Key,
                        Name = group.Key,
                        Count = group.Count()
                    })
                    .OrderBy(cat => cat.Name)
                    .ToListAsync();

                _logger.LogInformation("✅ Retrieved {Count} gallery categories", categories.Count);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error retrieving gallery categories");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while retrieving gallery categories. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🌐 Get public gallery images for website display
        /// </summary>
        [Function("GetPublicGalleryImages")]
        public async Task<IActionResult> GetPublicGalleryImages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gallery")] HttpRequest req)
        {
            _logger.LogInformation("🌐 Public gallery images request received");

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return new OkResult();
                }

                var categoryFilter = req.Query["category"].ToString();
                
                _logger.LogInformation("🔍 Retrieving public gallery images{CategoryFilter}", 
                    !string.IsNullOrEmpty(categoryFilter) ? $" for category: {categoryFilter}" : "");

                var query = _context.GalleryImages
                    .Where(img => img.IsActive); // Only show active images to public

                // Apply category filter if provided
                if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "all")
                {
                    query = query.Where(img => img.Category == categoryFilter);
                }

                var images = await query
                    .OrderBy(img => img.SortOrder)
                    .ThenByDescending(img => img.UploadedAt)
                    .Select(img => new GalleryImageResponseDto
                    {
                        Id = img.Id,
                        Src = img.Src,
                        Alt = img.Alt,
                        Category = img.Category,
                        Title = img.Title,
                        Description = img.Description,
                        IsActive = img.IsActive,
                        SortOrder = img.SortOrder,
                        UploadedAt = img.UploadedAt,
                        UpdatedAt = img.UpdatedAt
                    })
                    .ToListAsync();

                // Get all available categories for active images
                var categories = await _context.GalleryImages
                    .Where(img => img.IsActive)
                    .Select(img => img.Category)
                    .Distinct()
                    .OrderBy(cat => cat)
                    .ToListAsync();

                var response = new GalleryImageListResponseDto
                {
                    Success = true,
                    Data = images,
                    TotalCount = images.Count,
                    Categories = categories
                };

                _logger.LogInformation("✅ Retrieved {Count} public gallery images", images.Count);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while retrieving public gallery images");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while retrieving gallery images. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
