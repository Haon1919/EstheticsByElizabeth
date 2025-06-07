using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Attributes;

namespace API.Functions
{
    /// <summary>
    /// 📋 Contact Submissions Retriever 📋
    /// Handles retrieving contact form submissions for the admin panel.
    /// </summary>
    public class GetContactSubmissions
    {
        private readonly ILogger<GetContactSubmissions> _logger;
        private readonly ProjectContext _context;

        public GetContactSubmissions(ILogger<GetContactSubmissions> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 📊 The Admin Data Retrieval Magic 📊
        /// Azure Function triggered by HTTP GET to retrieve contact submissions for admin review.
        /// </summary>
        [Function("GetContactSubmissions")]
        [Cors]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "manage/contacts")] HttpRequest req)
        {
            _logger.LogInformation("📋 Contact submissions retrieval request received");

            // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
                req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
                req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
                req.HttpContext.Response.Headers["Access-Control-Max-Age"] = "86400";
                
                return response;
            }

            // Add CORS headers to all responses
            req.HttpContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
            req.HttpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, OPTIONS";
            req.HttpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";

            try
            {
                // Parse query parameters for filtering and pagination
                var statusFilter = req.Query["status"].ToString();
                var searchQuery = req.Query["search"].ToString();
                var pageStr = req.Query["page"].ToString();
                var pageSizeStr = req.Query["pageSize"].ToString();

                int page = int.TryParse(pageStr, out int p) && p > 0 ? p : 1;
                int pageSize = int.TryParse(pageSizeStr, out int ps) && ps > 0 && ps <= 100 ? ps : 50;

                var query = _context.ContactSubmissions.AsQueryable();

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var validStatuses = new[] { "unread", "read", "responded" };
                    if (Array.Exists(validStatuses, s => s == statusFilter))
                    {
                        query = query.Where(cs => cs.Status == statusFilter);
                    }
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    var search = searchQuery.ToLower();
                    query = query.Where(cs => 
                        cs.Name.ToLower().Contains(search) ||
                        cs.Email.ToLower().Contains(search) ||
                        cs.Subject.ToLower().Contains(search) ||
                        cs.InterestedService.ToLower().Contains(search));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination and ordering
                var submissionData = await query
                    .OrderByDescending(cs => cs.SubmittedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(cs => new
                    {
                        id = cs.Id,
                        name = cs.Name,
                        email = cs.Email,
                        phone = cs.Phone,
                        subject = cs.Subject,
                        message = cs.Message,
                        interestedService = cs.InterestedService,
                        preferredContactMethod = cs.PreferredContactMethod,
                        submittedAt = cs.SubmittedAt,
                        status = cs.Status,
                        readAt = cs.ReadAt,
                        respondedAt = cs.RespondedAt,
                        adminNotes = cs.AdminNotes
                    })
                    .ToListAsync();

                // Transform to final response format
                var submissions = submissionData.Select(cs => new
                {
                    id = cs.id.ToString(),
                    name = cs.name,
                    email = cs.email,
                    phone = cs.phone,
                    subject = cs.subject,
                    message = cs.message,
                    interestedService = cs.interestedService,
                    preferredContactMethod = cs.preferredContactMethod,
                    submittedAt = cs.submittedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    status = cs.status,
                    readAt = cs.readAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    respondedAt = cs.respondedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    adminNotes = cs.adminNotes
                }).ToList();

                _logger.LogInformation("✅ Retrieved {Count} contact submissions (page {Page} of {TotalPages})", 
                    submissions.Count, page, Math.Ceiling((double)totalCount / pageSize));

                return new OkObjectResult(new
                {
                    success = true,
                    data = submissions,
                    pagination = new
                    {
                        page = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = Math.Ceiling((double)totalCount / pageSize)
                    },
                    filters = new
                    {
                        status = statusFilter,
                        search = searchQuery
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error occurred while retrieving contact submissions");
                return new ObjectResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while retrieving contact submissions. Please try again later." 
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
