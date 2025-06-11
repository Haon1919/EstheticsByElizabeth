using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using API.DTOs;

namespace API.Functions
{
    /// <summary>
    /// 📤 Image Upload Handler 📤
    /// Handles image file uploads for the gallery system.
    /// </summary>
    public class UploadImage
    {
        private readonly ILogger<UploadImage> _logger;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public UploadImage(ILogger<UploadImage> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 📤 Upload gallery image file
        /// </summary>
        [Function("UploadGalleryImage")]
        public async Task<IActionResult> UploadGalleryImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/gallery/upload")] HttpRequest req)
        {
            _logger.LogInformation("📤 Image upload request received");

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

                // Check if the request contains a file
                if (!req.HasFormContentType)
                {
                    _logger.LogWarning("🚫 Request does not contain form data");
                    return new BadRequestObjectResult(new UploadImageResponseDto
                    {
                        Success = false,
                        Url = "",
                        Filename = "",
                        Message = "Request must contain form data with an image file."
                    });
                }

                var form = await req.ReadFormAsync();
                var file = form.Files["image"];

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("🚫 No image file provided in request");
                    return new BadRequestObjectResult(new UploadImageResponseDto
                    {
                        Success = false,
                        Url = "",
                        Filename = "",
                        Message = "No image file provided. Please select an image file to upload."
                    });
                }

                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    _logger.LogWarning("🚫 File size too large: {FileSize} bytes", file.Length);
                    return new BadRequestObjectResult(new UploadImageResponseDto
                    {
                        Success = false,
                        Url = "",
                        Filename = "",
                        Message = $"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)}MB."
                    });
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExtension) || !Array.Exists(AllowedExtensions, ext => ext == fileExtension))
                {
                    _logger.LogWarning("🚫 Invalid file extension: {Extension}", fileExtension);
                    return new BadRequestObjectResult(new UploadImageResponseDto
                    {
                        Success = false,
                        Url = "",
                        Filename = "",
                        Message = $"Invalid file type. Allowed types: {string.Join(", ", AllowedExtensions)}"
                    });
                }

                _logger.LogInformation("📤 Processing image upload: {FileName} ({FileSize} bytes)", 
                    file.FileName, file.Length);

                // Generate a unique filename to prevent conflicts
                var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N")[..8];
                var safeFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace("-", "_");
                var newFileName = $"{timestamp}_{uniqueId}_{safeFileName}{fileExtension}";

                // In a real-world scenario, you would upload to Azure Blob Storage, AWS S3, etc.
                // For this demo, we'll simulate the upload and return a mock URL
                // 
                // Example Azure Blob Storage implementation:
                // var blobServiceClient = new BlobServiceClient(connectionString);
                // var containerClient = blobServiceClient.GetBlobContainerClient("gallery-images");
                // var blobClient = containerClient.GetBlobClient(newFileName);
                // 
                // using var stream = file.OpenReadStream();
                // await blobClient.UploadAsync(stream, overwrite: true);
                // 
                // var imageUrl = blobClient.Uri.ToString();

                // For demo purposes, we'll save to local storage and return a mock URL
                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery");
                Directory.CreateDirectory(uploadsDirectory);

                var filePath = Path.Combine(uploadsDirectory, newFileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Generate URL (in production, this would be your CDN/blob storage URL)
                var baseUrl = $"{req.Scheme}://{req.Host}";
                var imageUrl = $"{baseUrl}/uploads/gallery/{newFileName}";

                _logger.LogInformation("✅ Image uploaded successfully: {FileName} -> {ImageUrl}", 
                    file.FileName, imageUrl);

                var response = new UploadImageResponseDto
                {
                    Success = true,
                    Url = imageUrl,
                    Filename = newFileName,
                    Message = "Image uploaded successfully."
                };

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error uploading image");
                return new ObjectResult(new UploadImageResponseDto
                {
                    Success = false,
                    Url = "",
                    Filename = "",
                    Message = "An error occurred while uploading the image. Please try again later."
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// 🔍 Get upload information and limits
        /// </summary>
        [Function("GetUploadInfo")]
        public IActionResult GetUploadInfo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "manage/gallery/upload/info")] HttpRequest req)
        {
            _logger.LogInformation("🔍 Upload info request received");

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

                var uploadInfo = new
                {
                    MaxFileSize = MaxFileSize,
                    MaxFileSizeMB = MaxFileSize / (1024 * 1024),
                    AllowedExtensions = AllowedExtensions,
                    AllowedMimeTypes = new[] 
                    { 
                        "image/jpeg", 
                        "image/png", 
                        "image/gif", 
                        "image/webp" 
                    }
                };

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");

                return new OkObjectResult(uploadInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error getting upload info");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An error occurred while retrieving upload information." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
