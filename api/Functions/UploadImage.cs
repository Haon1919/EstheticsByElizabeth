using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using API.DTOs;
using API.Services;

namespace API.Functions
{
    /// <summary>
    /// 📤 Image Upload Handler 📤
    /// Handles image file uploads for the gallery system.
    /// </summary>
    public class UploadImage
    {
        private readonly IAuthTokenService _authTokenService;
        private readonly ILogger<UploadImage> _logger;
        private readonly IImageStorageService _storageService;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        // File signatures (Magic Numbers)
        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".webp", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 }, new byte[] { 0x57, 0x45, 0x42, 0x50 } } } // WEBP is complex (RIFF...WEBP)
        };

        public UploadImage(ILogger<UploadImage> logger, IImageStorageService storageService, IAuthTokenService authTokenService)
        {
            _authTokenService = authTokenService;
            _logger = logger;
            _storageService = storageService;
        }

        /// <summary>
        /// 📤 Upload gallery image file
        /// </summary>
        [Function("UploadGalleryImage")]
        public async Task<IActionResult> UploadGalleryImage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/gallery/upload")] HttpRequest req)
        {
            _logger.LogInformation("📤 Image upload request received");

            if (!_authTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
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

                // Validate Magic Number (Content Inspection)
                if (!IsValidFileSignature(file, fileExtension))
                {
                    _logger.LogWarning("🚫 Invalid file signature for extension {Extension}", fileExtension);
                    return new BadRequestObjectResult(new UploadImageResponseDto
                    {
                        Success = false,
                        Url = "",
                        Filename = "",
                        Message = "Invalid file content. The file does not match its extension."
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

                // Upload using the storage service
                var imageUrl = await _storageService.UploadImageAsync(file, newFileName);

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

            if (!_authTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

            try
            {
                // Handle CORS preflight requests
                if (req.Method == "OPTIONS")
                {
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    req.HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
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
        private bool IsValidFileSignature(IFormFile file, string extension)
        {
            try
            {
                using var stream = file.OpenReadStream();
                if (stream.Length == 0) return false;

                // Read header bytes
                var headerBytes = new byte[12]; // Enough for most signatures
                stream.Read(headerBytes, 0, headerBytes.Length);

                // Reset stream position is unnecessary here because we open a new stream later in storage service
                // But good practice if checking allows seeking:
                // if (stream.CanSeek) stream.Position = 0;

                if (_fileSignatures.TryGetValue(extension, out var signatures))
                {
                    foreach (var signature in signatures)
                    {
                        // Check if header starts with signature
                        if (headerBytes.Take(signature.Length).SequenceEqual(signature))
                        {
                            // Special check for WEBP (RIFF....WEBP)
                            if (extension == ".webp")
                            {
                                // WEBP check is: RIFF (4 bytes) + size (4 bytes) + WEBP (4 bytes)
                                // We already checked RIFF (first 4 bytes). Now check WEBP at offset 8.
                                var webpMagic = new byte[] { 0x57, 0x45, 0x42, 0x50 };
                                if (headerBytes.Skip(8).Take(4).SequenceEqual(webpMagic))
                                {
                                    return true;
                                }
                                continue; 
                            }
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
