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
using API.DTOs;
using API.Entities;
using API.Attributes;

namespace API.Functions
{
    /// <summary>
    /// 📧 The Contact Form Handler 📧
    /// Handles contact form submissions from the website.
    /// </summary>
    public class SubmitContactForm
    {
        private readonly ILogger<SubmitContactForm> _logger;
        private readonly ProjectContext _context;

        public SubmitContactForm(ILogger<SubmitContactForm> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 📬 The Magical Contact Submission Ritual 📬
        /// Azure Function triggered by HTTP POST to handle contact form submissions.
        /// </summary>
        [Function("SubmitContactForm")]
        [Cors]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "contact")] HttpRequest req)
        {
            _logger.LogInformation("📧 Contact form submission received.");

            // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
                req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
                req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
                req.HttpContext.Response.Headers["Access-Control-Max-Age"] = "86400";
                
                return response;
            }

            // Add CORS headers to all responses
            req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
            req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "POST, OPTIONS";
            req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            try
            {
                // 📜 Decode the incoming request body for contact details
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    _logger.LogWarning("📭 Empty request body received");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Request body cannot be empty" 
                    });
                }

                var contactDto = JsonSerializer.Deserialize<ContactSubmissionDto>(
                    requestBody,
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                if (contactDto == null)
                {
                    _logger.LogWarning("📭 Invalid JSON in request body");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Invalid contact form data" 
                    });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(contactDto.Name) ||
                    string.IsNullOrWhiteSpace(contactDto.Email) ||
                    string.IsNullOrWhiteSpace(contactDto.Subject) ||
                    string.IsNullOrWhiteSpace(contactDto.Message))
                {
                    _logger.LogWarning("📝 Missing required fields in contact submission");
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "All required fields (Name, Email, Subject, Message) must be provided" 
                    });
                }

                // Basic email validation (additional to data annotation)
                if (!contactDto.Email.Contains("@") || !contactDto.Email.Contains("."))
                {
                    _logger.LogWarning("📧 Invalid email format: {Email}", contactDto.Email);
                    return new BadRequestObjectResult(new { 
                        success = false, 
                        message = "Please provide a valid email address" 
                    });
                }

                _logger.LogInformation("📝 Processing contact submission from {Name} ({Email})", 
                    contactDto.Name, contactDto.Email);

                // Create the contact submission entity
                var contactSubmission = new ContactSubmission
                {
                    Name = contactDto.Name.Trim(),
                    Email = contactDto.Email.Trim().ToLowerInvariant(),
                    Phone = contactDto.Phone?.Trim(),
                    Subject = contactDto.Subject.Trim(),
                    Message = contactDto.Message.Trim(),
                    InterestedService = string.IsNullOrWhiteSpace(contactDto.InterestedService) ? null : contactDto.InterestedService.Trim(),
                    PreferredContactMethod = contactDto.PreferredContactMethod?.Trim() ?? "Email",
                    SubmittedAt = DateTimeOffset.UtcNow,
                    Status = "unread"
                };

                // Save to database
                _context.ContactSubmissions.Add(contactSubmission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Contact submission saved successfully with ID {Id} from {Name}", 
                    contactSubmission.Id, contactSubmission.Name);

                return new OkObjectResult(new
                {
                    success = true,
                    message = "Thank you for your message! We will get back to you soon.",
                    submissionId = contactSubmission.Id,
                    timestamp = contactSubmission.SubmittedAt
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "📭 Failed to parse JSON in contact submission");
                return new BadRequestObjectResult(new { 
                    success = false, 
                    message = "Invalid JSON format in request" 
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "💾 Database error while saving contact submission");
                return new ObjectResult(new { 
                    success = false, 
                    message = "A database error occurred while processing your message. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while processing contact submission");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while processing your message. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
