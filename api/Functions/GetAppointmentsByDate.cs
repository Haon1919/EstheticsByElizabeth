using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;

using Microsoft.EntityFrameworkCore;
using API.Services;

namespace API.Functions
{    /// <summary>
    /// 📅 The Daily Schedule Oracle 📅
    /// Retrieves all appointments for a specific date or date range.
    /// Supports single date: /appointments/date/2024-01-15
    /// Supports date range: /appointments/date/2024-01-15?endDate=2024-01-20
    /// </summary>
    public class GetAppointmentsByDate
    {
        private readonly ILogger<GetAppointmentsByDate> _logger;
        private readonly ProjectContext _context;
        
        public GetAppointmentsByDate(ILogger<GetAppointmentsByDate> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));        }        /// <summary>
        /// 📅 The Magical Daily/Range Schedule Ritual 📅
        /// Azure Function triggered by HTTP GET to retrieve appointments for a specific date or date range.
        /// Supports both single date (/appointments/date/2024-01-15) and date range (/appointments/date/2024-01-15?endDate=2024-01-20)
        /// </summary>
        [Function("GetAppointmentsByDate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "appointments/date/{date}")] HttpRequest req,
            string date)        {
            // Check for date range parameters
            var endDateParam = req.Query["endDate"].ToString();
            var isDateRange = !string.IsNullOrEmpty(endDateParam);
            
            _logger.LogInformation("📅 Appointments request received for {RequestType}: {Date}{EndDate}",
                isDateRange ? "date range" : "single date",
                date,
                isDateRange ? $" to {endDateParam}" : "");

            if (!AuthTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }

            if (string.IsNullOrEmpty(date))
            {
                _logger.LogWarning("🚫 No date parameter provided.");
                return new BadRequestObjectResult("Please provide a date parameter (YYYY-MM-DD).");
            }

            try
            {
                // Parse start date
                if (!DateTime.TryParse(date, out DateTime parsedStartDate))
                {
                    _logger.LogWarning("🚫 Invalid start date format: {Date}", date);
                    return new BadRequestObjectResult("Invalid start date format. Please use YYYY-MM-DD.");
                }

                DateTime parsedEndDate = parsedStartDate;
                
                // Parse end date if provided (for date range)
                if (isDateRange)
                {
                    if (!DateTime.TryParse(endDateParam, out parsedEndDate))
                    {
                        _logger.LogWarning("🚫 Invalid end date format: {EndDate}", endDateParam);
                        return new BadRequestObjectResult("Invalid end date format. Please use YYYY-MM-DD.");
                    }
                    
                    // Validate date range
                    if (parsedEndDate < parsedStartDate)
                    {
                        _logger.LogWarning("🚫 End date cannot be before start date: {StartDate} to {EndDate}", 
                            parsedStartDate.ToString("yyyy-MM-dd"), parsedEndDate.ToString("yyyy-MM-dd"));
                        return new BadRequestObjectResult("End date cannot be before start date.");
                    }
                    
                    // Limit range to prevent excessive data requests (max 31 days)
                    var daysDifference = (parsedEndDate - parsedStartDate).Days;
                    if (daysDifference > 31)
                    {
                        _logger.LogWarning("🚫 Date range too large: {Days} days. Maximum allowed is 31 days.", daysDifference);
                        return new BadRequestObjectResult("Date range cannot exceed 31 days.");
                    }
                }

                var dateRangeText = isDateRange 
                    ? $"{parsedStartDate:yyyy-MM-dd} to {parsedEndDate:yyyy-MM-dd}"
                    : $"{parsedStartDate:yyyy-MM-dd}";
                    
                _logger.LogInformation("🔍 Searching for appointments on {DateRange}", dateRangeText);

                // Set up date range for query
                var startDate = new DateTimeOffset(parsedStartDate.Year, parsedStartDate.Month, parsedStartDate.Day, 0, 0, 0, TimeSpan.Zero);
                var endDate = new DateTimeOffset(parsedEndDate.Year, parsedEndDate.Month, parsedEndDate.Day, 23, 59, 59, 999, TimeSpan.Zero).AddMilliseconds(1);                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                        .ThenInclude(s => s.Category)
                    .Where(a => a.Time >= startDate && a.Time <= endDate)
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
                        },                        Service = new
                        {
                            a.Service.Id,
                            a.Service.Name,
                            a.Service.Description,
                            a.Service.Price,
                            a.Service.Duration,
                            a.Service.AppointmentBufferTime,
                            Category = new
                            {
                                a.Service.Category.Id,
                                a.Service.Category.Name
                            }
                        }
                    })
                    .ToListAsync();
                
                _logger.LogInformation("✅ Retrieved {Count} appointments for {DateRange}", 
                    appointments.Count, dateRangeText);
                
                // Build response object
                var response = new
                {
                    StartDate = parsedStartDate.ToString("yyyy-MM-dd"),
                    EndDate = parsedEndDate.ToString("yyyy-MM-dd"),
                    IsDateRange = isDateRange,
                    Appointments = appointments,
                    TotalCount = appointments.Count
                };
                
                return new OkObjectResult(response);
            }            // Catch any unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving appointments for date range {Date}!", date);
                return new ObjectResult("An unexpected error occurred while retrieving appointments.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
