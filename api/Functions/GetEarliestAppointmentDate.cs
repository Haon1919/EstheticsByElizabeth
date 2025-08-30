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
    /// 📅 The Earliest Date Finder 📅
    /// Retrieves the earliest date that has appointments scheduled.
    /// </summary>
    public class GetEarliestAppointmentDate
    {
        private readonly ILogger<GetEarliestAppointmentDate> _logger;
        private readonly ProjectContext _context;
        
        public GetEarliestAppointmentDate(ILogger<GetEarliestAppointmentDate> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🔍 The Earliest Date Discovery Ritual 🔍
        /// Azure Function triggered by HTTP GET to find the earliest appointment date.
        /// Returns the earliest date with appointments, or null if no appointments exist.
        /// </summary>        
        [Function("GetEarliestAppointmentDate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "appointments/earliest-date")] HttpRequest req)
        {
            _logger.LogInformation("🔍 Request received to find earliest appointment date.");

            try
            {
                // Find the earliest appointment date
                var earliestAppointment = await _context.Appointments
                    .AsNoTracking()
                    .OrderBy(a => a.Time)
                    .FirstOrDefaultAsync();

                if (earliestAppointment == null)
                {
                    _logger.LogInformation("📅 No appointments found in the system.");
                    return new OkObjectResult(new 
                    {
                        EarliestDate = (string?)null,
                        HasAppointments = false,
                        Message = "No appointments found"
                    });
                }

                var earliestDate = earliestAppointment.Time.ToString("yyyy-MM-dd");
                
                _logger.LogInformation("✅ Found earliest appointment date: {EarliestDate}", earliestDate);
                
                return new OkObjectResult(new 
                {
                    EarliestDate = earliestDate,
                    HasAppointments = true,
                    Message = "Earliest appointment date found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while finding earliest appointment date!");
                return new ObjectResult("An unexpected error occurred while finding earliest appointment date.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
