using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Entities;
using API.Attributes;

namespace API.Functions
{
    /// <summary>
    /// 📝 Contact Submission Status Updater 📝
    /// Handles updating the status of contact form submissions in the admin panel.
    /// </summary>
    public class UpdateContactSubmissionStatus
    {
        private readonly ILogger<UpdateContactSubmissionStatus> _logger;
        private readonly ProjectContext _context;

        public UpdateContactSubmissionStatus(ILogger<UpdateContactSubmissionStatus> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🔄 The Status Update Magic 🔄
        /// Azure Function triggered by HTTP PUT to update contact submission status.
        /// </summary>
        [Function("UpdateContactSubmissionStatus")]
        [Cors]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", "options", Route = "contact/{id}/status")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("📝 Contact submission status update request received for ID: {Id}", id);

            // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
                req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "PUT, OPTIONS";
                req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
                req.HttpContext.Response.Headers["Access-Control-Max-Age"] = "86400";
                
                return response;
            }

            // Add CORS headers to all responses
            req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
            req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "PUT, OPTIONS";
            req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            try
            {
                // Validate ID parameter
                if (!int.TryParse(id, out int submissionId) || submissionId <= 0)
                {
                    _logger.LogWarning("📭 Invalid submission ID: {Id}", id);
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Invalid submission ID" 
                    });
                }

                // Read request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("📭 Empty request body received");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Request body cannot be empty" 
                    });
                }

                var updateRequest = JsonSerializer.Deserialize<UpdateStatusRequest>(
                    requestBody,
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                if (updateRequest == null || string.IsNullOrWhiteSpace(updateRequest.Status))
                {
                    _logger.LogWarning("📭 Invalid request data");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Status is required" 
                    });
                }

                // Validate status value
                var validStatuses = new[] { "unread", "read", "responded" };
                if (!Array.Exists(validStatuses, s => s == updateRequest.Status))
                {
                    _logger.LogWarning("📭 Invalid status value: {Status}", updateRequest.Status);
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Status must be 'unread', 'read', or 'responded'" 
                    });
                }

                // Find the contact submission
                var submission = await _context.ContactSubmissions
                    .FirstOrDefaultAsync(cs => cs.Id == submissionId);

                if (submission == null)
                {
                    _logger.LogWarning("📭 Contact submission not found: {Id}", submissionId);
                    return new NotFoundObjectResult(new { 
                        success = false, 
                        message = "Contact submission not found" 
                    });
                }

                var now = DateTimeOffset.UtcNow;
                var previousStatus = submission.Status;

                // Update the status and set appropriate timestamps
                submission.Status = updateRequest.Status;

                if (updateRequest.Status == "read" && previousStatus == "unread")
                {
                    submission.ReadAt = now;
                }
                else if (updateRequest.Status == "responded")
                {
                    if (submission.ReadAt == null)
                    {
                        submission.ReadAt = now;
                    }
                    submission.RespondedAt = now;
                }

                // Update admin notes if provided
                if (!string.IsNullOrWhiteSpace(updateRequest.AdminNotes))
                {
                    submission.AdminNotes = updateRequest.AdminNotes.Trim();
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contact submission {Id} status updated from {PreviousStatus} to {NewStatus}", 
                    submissionId, previousStatus, updateRequest.Status);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Status updated successfully",
                    submissionId = submission.Id,
                    previousStatus = previousStatus,
                    newStatus = submission.Status,
                    updatedAt = now
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "📭 Failed to parse JSON in status update request");
                return new BadRequestObjectResult(new { 
                    success = false, 
                    message = "Invalid JSON format in request" 
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "💾 Database error while updating contact submission status");
                return new ObjectResult(new { 
                    success = false, 
                    message = "A database error occurred while updating the status. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while updating contact submission status");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while updating the status. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? AdminNotes { get; set; }
    }
}
