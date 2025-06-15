using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace API.Services
{
    /// <summary>
    /// 🪣 HTTP-based MinIO Image Storage Service 🪣
    /// Direct HTTP implementation for MinIO S3-compatible API
    /// This bypasses the MinIO .NET client which has compatibility issues in Docker
    /// </summary>
    public class HttpMinioImageStorageService : IImageStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpMinioImageStorageService> _logger;
        private readonly string _bucketName;
        private readonly string _endpoint;
        private readonly string _publicEndpoint;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly bool _useSSL;

        public HttpMinioImageStorageService(IConfiguration configuration, ILogger<HttpMinioImageStorageService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _bucketName = configuration["Values:ImageStorage:MinIO:BucketName"] ?? "gallery-images";
            _endpoint = configuration["Values:ImageStorage:MinIO:Endpoint"] ?? "localhost:9000";
            _publicEndpoint = configuration["Values:ImageStorage:MinIO:PublicEndpoint"] ?? "localhost:9000";
            _accessKey = configuration["Values:ImageStorage:MinIO:AccessKey"] ?? "minioadmin";
            _secretKey = configuration["Values:ImageStorage:MinIO:SecretKey"] ?? "minioadmin123";
            _useSSL = bool.Parse(configuration["Values:ImageStorage:MinIO:UseSSL"] ?? "false");

            _logger.LogInformation("🪣 Initializing HTTP-based MinIO client - Endpoint: {Endpoint}, PublicEndpoint: {PublicEndpoint}, Bucket: {Bucket}, UseSSL: {UseSSL}", 
                _endpoint, _publicEndpoint, _bucketName, _useSSL);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string fileName)
        {
            _logger.LogInformation("🪣 Uploading image to MinIO via HTTP: {FileName} ({FileSize} bytes)", fileName, file.Length);

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            try
            {
                var objectKey = $"images/gallery/{fileName}";
                var protocol = _useSSL ? "https" : "http";
                var baseUrl = $"{protocol}://{_endpoint}";
                var uploadUrl = $"{baseUrl}/{_bucketName}/{objectKey}";

                _logger.LogInformation("📤 Uploading to URL: {UploadUrl}", uploadUrl);

                // Create temporary file
                var tempFilePath = Path.GetTempFileName();
                try
                {
                    _logger.LogInformation("💾 Saving file to temporary location: {TempPath}", tempFilePath);
                    
                    using (var tempFileStream = File.Create(tempFilePath))
                    {
                        using var sourceStream = file.OpenReadStream();
                        await sourceStream.CopyToAsync(tempFileStream);
                    }
                    
                    var tempFileInfo = new FileInfo(tempFilePath);
                    _logger.LogInformation("📊 Temporary file created: Size={Size} bytes", tempFileInfo.Length);
                    
                    if (tempFileInfo.Length == 0)
                    {
                        throw new InvalidOperationException("Temporary file is empty after copy");
                    }

                    // Read file content for HTTP upload
                    var fileContent = await File.ReadAllBytesAsync(tempFilePath);
                    
                    // Create HTTP content
                    using var content = new ByteArrayContent(fileContent);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    content.Headers.ContentLength = fileContent.Length;

                    // Add AWS S3 signature headers (simplified for MinIO)
                    var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
                    {
                        Content = content
                    };

                    // Add basic auth headers for MinIO
                    var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accessKey}:{_secretKey}"));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                    _logger.LogInformation("🚀 Executing HTTP PUT request...");
                    
                    var response = await _httpClient.SendAsync(request);
                    
                    _logger.LogInformation("📨 HTTP Response: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ Upload successful via HTTP");
                        
                        // Verify upload by checking if file exists
                        var verifyUrl = $"{baseUrl}/{_bucketName}/{objectKey}";
                        var headRequest = new HttpRequestMessage(HttpMethod.Head, verifyUrl);
                        headRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
                        
                        var headResponse = await _httpClient.SendAsync(headRequest);
                        if (headResponse.IsSuccessStatusCode)
                        {
                            var contentLength = headResponse.Content.Headers.ContentLength ?? 0;
                            _logger.LogInformation("✅ Upload verified - File size: {Size} bytes", contentLength);
                            
                            if (contentLength == 0)
                            {
                                throw new InvalidOperationException("Upload verification failed - file size is 0 bytes");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Upload verification failed - HEAD request returned: {StatusCode}", headResponse.StatusCode);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("💥 HTTP Upload failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                        throw new InvalidOperationException($"HTTP upload failed: {response.StatusCode} - {errorContent}");
                    }
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        _logger.LogInformation("🗑️ Temporary file cleaned up: {TempPath}", tempFilePath);
                    }
                }

                _logger.LogInformation("✅ Image uploaded successfully via HTTP: {FileName} -> {ObjectKey}", fileName, objectKey);
                return objectKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to upload image via HTTP: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string objectKey)
        {
            _logger.LogInformation("🗑️ Deleting image from MinIO via HTTP: {ObjectKey}", objectKey);

            try
            {
                var protocol = _useSSL ? "https" : "http";
                var deleteUrl = $"{protocol}://{_endpoint}/{_bucketName}/{objectKey}";

                var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accessKey}:{_secretKey}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Image deleted successfully via HTTP: {ObjectKey}", objectKey);
                    return true;
                }
                else
                {
                    _logger.LogWarning("⚠️ Failed to delete image via HTTP: {StatusCode} - {ObjectKey}", response.StatusCode, objectKey);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error deleting image via HTTP: {ObjectKey}", objectKey);
                return false;
            }
        }

        public async Task<Stream> GetImageAsync(string fileName)
        {
            _logger.LogInformation("📥 Retrieving image from MinIO via HTTP: {FileName}", fileName);

            try
            {
                var protocol = _useSSL ? "https" : "http";
                var imageUrl = $"{protocol}://{_endpoint}/{_bucketName}/{fileName}";

                var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accessKey}:{_secretKey}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    _logger.LogInformation("✅ Image retrieved successfully via HTTP: {FileName}", fileName);
                    return stream;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to retrieve image: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to retrieve image via HTTP: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> ImageExistsAsync(string objectKey)
        {
            try
            {
                var protocol = _useSSL ? "https" : "http";
                var checkUrl = $"{protocol}://{_endpoint}/{_bucketName}/{objectKey}";

                var request = new HttpRequestMessage(HttpMethod.Head, checkUrl);
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accessKey}:{_secretKey}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                var response = await _httpClient.SendAsync(request);
                
                _logger.LogInformation("🔍 Image exists check via HTTP: {ObjectKey} - {StatusCode}", objectKey, response.StatusCode);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ Error checking if image exists via HTTP: {ObjectKey}", objectKey);
                return false;
            }
        }

        public string GetImageUrl(string objectKey)
        {
            _logger.LogInformation("🔗 Generating image URL for object: {ObjectKey}", objectKey);
            
            try
            {
                if (objectKey.StartsWith("http://") || objectKey.StartsWith("https://"))
                {
                    return objectKey;
                }

                var protocol = _useSSL ? "https" : "http";
                var imageUrl = $"{protocol}://{_publicEndpoint}/{_bucketName}/{objectKey}";

                _logger.LogInformation("✅ Generated MinIO image URL via HTTP: {ObjectKey} -> {ImageUrl}", objectKey, imageUrl);
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to generate image URL via HTTP: {ObjectKey}", objectKey);
                throw;
            }
        }
    }
}
