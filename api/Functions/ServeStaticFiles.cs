using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using API.Services;

namespace API.Functions
{
    /// <summary>
    /// 📁 Static File Server 📁
    /// Serves uploaded images and other static files
    /// </summary>
    public class ServeStaticFiles
    {
        private readonly ILogger<ServeStaticFiles> _logger;
        private readonly IImageStorageService _storageService;

        public ServeStaticFiles(ILogger<ServeStaticFiles> logger, IImageStorageService storageService)
        {
            _logger = logger;
            _storageService = storageService;
        }

        /// <summary>
        /// 📄 Serve static files from the uploads directory
        /// </summary>
        [Function("ServeUploadedFile")]
        public async Task<IActionResult> ServeUploadedFile(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "uploads/{fileName}")] HttpRequest req,
            string fileName)
        {
            _logger.LogInformation("📄 Static file request for: {FileName}", fileName);

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

                // Validate filename (security check)
                if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    _logger.LogWarning("🚫 Invalid filename requested: {FileName}", fileName);
                    return new NotFoundResult();
                }

                // Check if file exists
                var exists = await _storageService.ImageExistsAsync(fileName);
                if (!exists)
                {
                    _logger.LogWarning("📄 File not found: {FileName}", fileName);
                    return new NotFoundResult();
                }

                // Get the file stream
                var fileStream = await _storageService.GetImageAsync(fileName);
                
                // Determine content type based on file extension
                var contentType = GetContentType(fileName);
                
                _logger.LogInformation("✅ Serving file: {FileName} ({ContentType})", fileName, contentType);

                // Add CORS headers
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Cache-Control", "public, max-age=31536000"); // Cache for 1 year
                
                return new FileStreamResult(fileStream, contentType)
                {
                    EnableRangeProcessing = true // Enable partial content requests
                };
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("📄 File not found: {FileName}", fileName);
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error serving file: {FileName}", fileName);
                return new ObjectResult("Internal server error while serving file.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }

        /// <summary>
        /// Get MIME content type based on file extension
        /// </summary>
        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }
    }
}
