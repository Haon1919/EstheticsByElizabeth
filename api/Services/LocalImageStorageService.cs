using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    /// <summary>
    /// 📁 Local Image Storage Service 📁
    /// Implementation for local file system storage
    /// </summary>
    public class LocalImageStorageService : IImageStorageService
    {
        private readonly ILogger<LocalImageStorageService> _logger;
        private readonly string _uploadsPath;
        private readonly string _baseUrl;

        public LocalImageStorageService(IConfiguration configuration, ILogger<LocalImageStorageService> logger)
        {
            _logger = logger;
            _uploadsPath = configuration["ImageStorage:LocalPath"] ?? 
                          Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "gallery");
            _baseUrl = configuration["ImageStorage:BaseUrl"] ?? "http://localhost/uploads/gallery";

            // Ensure the uploads directory exists
            Directory.CreateDirectory(_uploadsPath);

            _logger.LogInformation("📁 Local storage initialized - Path: {UploadsPath}, BaseUrl: {BaseUrl}", 
                _uploadsPath, _baseUrl);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string fileName)
        {
            _logger.LogInformation("📁 Uploading image locally: {FileName} ({FileSize} bytes)", fileName, file.Length);

            try
            {
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var imageUrl = $"{_baseUrl.TrimEnd('/')}/{fileName}";

                _logger.LogInformation("✅ Image uploaded successfully locally: {FileName} -> {ImageUrl}", fileName, imageUrl);
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to upload image locally: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string fileName)
        {
            _logger.LogInformation("🗑️ Deleting image locally: {FileName}", fileName);

            try
            {
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("✅ Image deleted successfully locally: {FileName}", fileName);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ Image file not found for deletion: {FileName}", fileName);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to delete image locally: {FileName}", fileName);
                return false;
            }
        }

        public async Task<Stream> GetImageAsync(string fileName)
        {
            _logger.LogInformation("📥 Retrieving image locally: {FileName}", fileName);

            try
            {
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Image file not found: {fileName}");
                }

                var stream = File.OpenRead(filePath);
                _logger.LogInformation("✅ Image retrieved successfully locally: {FileName}", fileName);
                
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to retrieve image locally: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> ImageExistsAsync(string fileName)
        {
            var filePath = Path.Combine(_uploadsPath, fileName);
            return File.Exists(filePath);
        }

        public string GetImageUrl(string fileName)
        {
            // Return the relative URL for local storage
            return $"/api/uploads/{fileName}";
        }
    }
}
