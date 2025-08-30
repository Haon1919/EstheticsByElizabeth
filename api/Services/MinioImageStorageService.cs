using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace API.Services
{
    /// <summary>
    /// 🪣 MinIO Image Storage Service 🪣
    /// Implementation for MinIO (S3-compatible) object storage
    /// </summary>
    public class MinioImageStorageService : IImageStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly ILogger<MinioImageStorageService> _logger;
        private readonly string _bucketName;
        private readonly string _endpoint;
        private readonly string _publicEndpoint;
        private readonly bool _useSSL;

        public MinioImageStorageService(IMinioClient minioClient, IConfiguration configuration, ILogger<MinioImageStorageService> logger)
        {
            _minioClient = minioClient;
            _logger = logger;
            _bucketName = configuration["Values:ImageStorage:MinIO:BucketName"] ?? "gallery-images";
            _endpoint = configuration["Values:ImageStorage:MinIO:Endpoint"] ?? "localhost:9000";
            _publicEndpoint = configuration["Values:ImageStorage:MinIO:PublicEndpoint"] ?? "localhost:9000";
            _useSSL = bool.Parse(configuration["Values:ImageStorage:MinIO:UseSSL"] ?? "false");

            _logger.LogInformation("🪣 Initializing MinIO service with injected client - Bucket: {Bucket}, PublicEndpoint: {PublicEndpoint}, UseSSL: {UseSSL}", 
                _bucketName, _publicEndpoint, _useSSL);

            // Test connectivity by ensuring bucket exists
            _ = Task.Run(EnsureBucketExistsAsync);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string fileName)
        {
            _logger.LogInformation("🪣 Uploading image to MinIO: {FileName} ({FileSize} bytes)", fileName, file.Length);

            // Validate file before proceeding
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            try
            {
                // Ensure bucket exists before upload
                await EnsureBucketExistsAsync();

                // Use consistent path structure for all uploaded images
                var objectKey = $"images/gallery/{fileName}";

                if (file.Length == 0)
                {
                    throw new InvalidOperationException("Source file appears to be empty");
                }
                
                // Create temporary file for reliable upload
                var tempFilePath = Path.GetTempFileName();
                try
                {
                    
                    // Save IFormFile to temporary file
                    using (var tempFileStream = File.Create(tempFilePath))
                    {
                        using var sourceStream = file.OpenReadStream();
                        await sourceStream.CopyToAsync(tempFileStream);
                    }
                    
                    // Verify temp file was created with correct size
                    var tempFileInfo = new FileInfo(tempFilePath);
                    
                    if (tempFileInfo.Length == 0)
                    {
                        throw new InvalidOperationException("Temporary file is empty after copy");
                    }
                    
                    // Upload to MinIO using official client
                    using var tempStream = File.OpenRead(tempFilePath);
                    var putObjectArgs = new PutObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject(objectKey)
                        .WithStreamData(tempStream)
                        .WithObjectSize(tempFileInfo.Length)
                        .WithContentType(file.ContentType ?? "application/octet-stream");

                    var putResult = await _minioClient.PutObjectAsync(putObjectArgs);
                    
                    // Verify upload succeeded
                    try 
                    {
                        var statObjectArgs = new StatObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(objectKey);
                        
                        var statResult = await _minioClient.StatObjectAsync(statObjectArgs);
                            
                        if (statResult.Size == 0)
                        {
                            throw new InvalidOperationException($"Upload appears to have failed - object size is 0 bytes");
                        }
                    }
                    catch (Exception verifyEx)
                    {
                        _logger.LogError(verifyEx, "💥 Object verification failed after upload - upload may not have succeeded");
                        throw new InvalidOperationException("Upload verification failed - the file may not have been uploaded correctly", verifyEx);
                    }
                }
                finally
                {
                    // Clean up temporary file
                    try
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "⚠️ Failed to clean up temporary file: {TempPath}", tempFilePath);
                    }
                }

                // Return the object key for consistent URL generation
                _logger.LogInformation("✅ Image uploaded successfully to MinIO: {FileName} -> {ObjectKey}", fileName, objectKey);
                
                return objectKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to upload image to MinIO: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string objectKey)
        {
            _logger.LogInformation("🗑️ Deleting image from MinIO: {ObjectKey}", objectKey);

            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);

                _logger.LogInformation("✅ Image deleted successfully from MinIO: {ObjectKey}", objectKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to delete image from MinIO: {ObjectKey}", objectKey);
                return false;
            }
        }

        public async Task<Stream> GetImageAsync(string fileName)
        {
            _logger.LogInformation("📥 Retrieving image from MinIO: {FileName}", fileName);

            try
            {
                var stream = new MemoryStream();
                
                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithCallbackStream(objectStream => objectStream.CopyTo(stream));

                await _minioClient.GetObjectAsync(getObjectArgs);
                
                stream.Position = 0;
                _logger.LogInformation("✅ Image retrieved successfully from MinIO: {FileName}", fileName);
                
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to retrieve image from MinIO: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> ImageExistsAsync(string objectKey)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectKey);

                var statResult = await _minioClient.StatObjectAsync(statObjectArgs);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Object does not exist or error checking: {Object}, Error: {Error}", objectKey, ex.Message);
                return false;
            }
        }

        public string GetImageUrl(string objectKey)
        {
            try
            {
                // If objectKey is already a full URL, return it as-is
                if (objectKey.StartsWith("http://") || objectKey.StartsWith("https://"))
                {
                    return objectKey;
                }

                // Generate public URL using the public endpoint
                var protocol = _useSSL ? "https" : "http";
                var imageUrl = $"{protocol}://{_publicEndpoint}/{_bucketName}/{objectKey}";

                _logger.LogInformation("Generated MinIO image URL: {ObjectKey} -> {ImageUrl}", objectKey, imageUrl);
                
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to generate image URL for object: {ObjectKey}", objectKey);
                throw;
            }
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_bucketName);

                bool bucketExists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                if (!bucketExists)
                {
                    _logger.LogInformation("Creating bucket: {Bucket}", _bucketName);
                    
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_bucketName);

                    await _minioClient.MakeBucketAsync(makeBucketArgs);
                    
                    _logger.LogInformation("✅ Bucket created successfully: {Bucket}", _bucketName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to ensure bucket exists: {Bucket}", _bucketName);
                throw;
            }
        }
    }
}
