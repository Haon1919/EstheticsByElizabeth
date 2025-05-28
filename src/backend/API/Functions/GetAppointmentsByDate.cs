using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class GetAppointmentsByDate
    {
        private readonly ILogger<GetAppointmentsByDate> _logger;
        private readonly ProjectContext _context;

        public GetAppointmentsByDate(ILogger<GetAppointmentsByDate> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetAppointmentsByDate")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("GetAppointmentsByDate function processing request.");
            
            // Get the date from the query string
            string dateParam = req.Query["date"];
            
            if (string.IsNullOrEmpty(dateParam))
            {
                _logger.LogWarning("No date parameter provided.");
                return new BadRequestObjectResult("Please provide a date parameter (YYYY-MM-DD).");
            }

            try
            {
                if (!DateTime.TryParse(dateParam, out DateTime date))
                {
                    _logger.LogWarning("Invalid date format: {DateParam}", dateParam);
                    return new BadRequestObjectResult("Invalid date format. Please use YYYY-MM-DD.");
                }

                // Get appointments for the specified date
                var startDate = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
                var endDate = startDate.AddDays(1);

                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                        .ThenInclude(s => s.Category)
                    .Where(a => a.Time >= startDate && a.Time < endDate)
                    .OrderBy(a => a.Time)
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} appointments for date {Date}", appointments.Count, dateParam);
                
                return new OkObjectResult(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for date {Date}", dateParam);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
