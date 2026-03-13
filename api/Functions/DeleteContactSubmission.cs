using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Services;


namespace API.Functions
{
    /// <summary>
    /// 🗑️ Contact Submission Deleter 🗑️
    /// Handles deleting contact form submissions from the admin panel.
    /// </summary>
    public class DeleteContactSubmission
    {
        private readonly IAuthTokenService _authTokenService;
        private readonly ILogger<DeleteContactSubmission> _logger;
        private readonly ProjectContext _context;

        public DeleteContactSubmission(ILogger<DeleteContactSubmission> logger, ProjectContext context, IAuthTokenService authTokenService)
        {
            _authTokenService = authTokenService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🗑️ The Deletion Magic 🗑️
        /// Azure Function triggered by HTTP DELETE to remove contact submissions.
        /// </summary>
        [Function("DeleteContactSubmission")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/contacts/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("🗑️ Contact submission deletion request received for ID: {Id}", id);

            if (!_authTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

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

                // Log the submission details before deletion for audit purposes
                _logger.LogInformation("🗑️ Deleting contact submission from {Name} ({Email}) submitted at {SubmittedAt}", 
                    submission.Name, submission.Email, submission.SubmittedAt);

                // Remove the submission
                _context.ContactSubmissions.Remove(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contact submission {Id} deleted successfully", submissionId);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Contact submission deleted successfully",
                    submissionId = submissionId,
                    deletedAt = DateTimeOffset.UtcNow
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "💾 Database error while deleting contact submission");
                return new ObjectResult(new { 
                    success = false, 
                    message = "A database error occurred while deleting the submission. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while deleting contact submission");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while deleting the submission. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
