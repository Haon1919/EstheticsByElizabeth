using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Functions
{
    /// <summary>
    /// 📅 The Daily Schedule Oracle 📅
    /// Retrieves all appointments for a specific date.
    /// </summary>
    public class GetAppointmentsByDate
    {
        private readonly ILogger<GetAppointmentsByDate> _logger;
        private readonly ProjectContext _context;        public GetAppointmentsByDate(ILogger<GetAppointmentsByDate> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 📅 The Magical Daily Schedule Ritual 📅
        /// Azure Function triggered by HTTP GET to retrieve appointments for a specific date.
        /// </summary>
        [Function("GetAppointmentsByDate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "appointments/date/{date}")] HttpRequest req,
            string date)
        {
            _logger.LogInformation("📅 Daily appointments request received for date: {Date}", date);

            // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                req.HttpContext.Response.Headers.Add("Access-Control-Max-Age", "86400");
                
                return response;
            }

            // Add CORS headers to all responses
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            
            if (string.IsNullOrEmpty(date))
            {
                _logger.LogWarning("🚫 No date parameter provided.");
                return new BadRequestObjectResult("Please provide a date parameter (YYYY-MM-DD).");
            }            try
            {
                if (!DateTime.TryParse(date, out DateTime parsedDate))
                {
                    _logger.LogWarning("🚫 Invalid date format: {Date}", date);
                    return new BadRequestObjectResult("Invalid date format. Please use YYYY-MM-DD.");
                }

                _logger.LogInformation("🔍 Searching for appointments on date: {Date}", parsedDate.ToString("yyyy-MM-dd"));

                // Get appointments for the specified date
                var startDate = new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, TimeSpan.Zero);
                var endDate = startDate.AddDays(1);

                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                        .ThenInclude(s => s.Category)
                    .Where(a => a.Time >= startDate && a.Time < endDate)
                    .OrderBy(a => a.Time)
                    .Select(a => new
                    {
                        a.Id,
                        a.Time,
                        Client = new
                        {
                            a.Client.Id,
                            a.Client.FirstName,
                            a.Client.LastName,
                            a.Client.Email,
                            a.Client.PhoneNumber
                        },
                        Service = new
                        {
                            a.Service.Id,
                            a.Service.Name,
                            a.Service.Description,
                            a.Service.Price,
                            a.Service.Duration,
                            Category = new
                            {
                                a.Service.Category.Id,
                                a.Service.Category.Name
                            }
                        }
                    })
                    .ToListAsync();
                
                _logger.LogInformation("✅ Retrieved {Count} appointments for date {Date}", 
                    appointments.Count, parsedDate.ToString("yyyy-MM-dd"));
                
                return new OkObjectResult(new
                {
                    Date = parsedDate.ToString("yyyy-MM-dd"),
                    Appointments = appointments,
                    TotalCount = appointments.Count
                });
            }
            // Catch any unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving appointments for date {Date}!", date);
                return new ObjectResult("An unexpected error occurred while retrieving appointments.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
