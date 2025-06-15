using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace API.Services
{
    /// <summary>
    /// 🗄️ Image Storage Service Interface 🗄️
    /// Abstraction for image storage operations (local, MinIO, Azure Blob, etc.)
    /// </summary>
    public interface IImageStorageService
    {
        /// <summary>
        /// Upload an image file and return the public URL
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="fileName">The desired filename (without path)</param>
        /// <returns>The public URL to access the uploaded image</returns>
        Task<string> UploadImageAsync(IFormFile file, string fileName);

        /// <summary>
        /// Delete an image file
        /// </summary>
        /// <param name="fileName">The filename to delete (without path)</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        Task<bool> DeleteImageAsync(string fileName);

        /// <summary>
        /// Get an image file as a stream
        /// </summary>
        /// <param name="fileName">The filename to retrieve (without path)</param>
        /// <returns>A stream containing the image data</returns>
        Task<Stream> GetImageAsync(string fileName);

        /// <summary>
        /// Check if an image file exists
        /// </summary>
        /// <param name="fileName">The filename to check (without path)</param>
        /// <returns>True if the file exists, false otherwise</returns>
        Task<bool> ImageExistsAsync(string fileName);

        /// <summary>
        /// Get the public URL for an image
        /// </summary>
        /// <param name="fileName">The filename to get URL for (without path)</param>
        /// <returns>The public URL to access the image</returns>
        string GetImageUrl(string fileName);
    }
}
