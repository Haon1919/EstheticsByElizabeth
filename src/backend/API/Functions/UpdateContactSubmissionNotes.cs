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


namespace API.Functions
{
    /// <summary>
    /// 📝 Contact Submission Notes Updater 📝
    /// Handles updating admin notes for contact form submissions.
    /// </summary>
    public class UpdateContactSubmissionNotes
    {
        private readonly ILogger<UpdateContactSubmissionNotes> _logger;
        private readonly ProjectContext _context;

        public UpdateContactSubmissionNotes(ILogger<UpdateContactSubmissionNotes> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 📝 The Notes Update Magic 📝
        /// Azure Function triggered by HTTP PUT to update contact submission notes.
        /// </summary>
        [Function("UpdateContactSubmissionNotes")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "manage/contacts/{id}/notes")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("📝 Contact submission notes update request received for ID: {Id}", id);

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

                var updateRequest = JsonSerializer.Deserialize<UpdateNotesRequest>(
                    requestBody,
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                if (updateRequest == null)
                {
                    _logger.LogWarning("📭 Invalid request data");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Invalid request data" 
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

                var previousNotes = submission.AdminNotes;

                // Update admin notes
                submission.AdminNotes = string.IsNullOrWhiteSpace(updateRequest.AdminNotes) 
                    ? null 
                    : updateRequest.AdminNotes.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contact submission {Id} notes updated", submissionId);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Notes updated successfully",
                    submissionId = submission.Id,
                    updatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "📭 Failed to parse JSON in notes update request");
                return new BadRequestObjectResult(new { 
                    success = false, 
                    message = "Invalid JSON format in request" 
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "💾 Database error while updating contact submission notes");
                return new ObjectResult(new { 
                    success = false, 
                    message = "A database error occurred while updating the notes. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while updating contact submission notes");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while updating the notes. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }

    public class UpdateNotesRequest
    {
        public string AdminNotes { get; set; } = string.Empty;
    }
}
