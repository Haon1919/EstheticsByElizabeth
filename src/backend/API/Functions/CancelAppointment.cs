using System;
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
    /// 🗑️ The Appointment Cancellation Wizard 🗑️
    /// Handles cancelling existing appointments.
    /// </summary>
    public class CancelAppointment
    {
        private readonly ILogger<CancelAppointment> _logger;
        private readonly ProjectContext _context;        public CancelAppointment(ILogger<CancelAppointment> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 🗑️ The Magical Appointment Cancellation Ritual 🗑️
        /// Azure Function triggered by HTTP DELETE to cancel an appointment.
        /// </summary>
        [Function("CancelAppointment")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", "options", Route = "appointments/{id}")] HttpRequest req,
            string id)
        {
            _logger.LogInformation("🗑️ Appointment cancellation request received for ID: {Id}", id);

            // Handle CORS preflight request
            if (req.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("🌐 Handling CORS preflight request");
                
                var response = new OkResult();
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "DELETE, OPTIONS");
                req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                req.HttpContext.Response.Headers.Add("Access-Control-Max-Age", "86400");
                
                return response;
            }

            // Add CORS headers to all responses
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Methods", "DELETE, OPTIONS");
            req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            
            if (string.IsNullOrEmpty(id) || !int.TryParse(id, out int appointmentId))
            {
                _logger.LogWarning("🚫 Invalid or missing appointment ID: {Id}", id);
                return new BadRequestObjectResult("Please provide a valid appointment ID.");
            }            try
            {
                _logger.LogInformation("🔍 Searching for appointment with ID: {Id}", appointmentId);
                
                // Find the appointment by ID
                var appointment = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
                if (appointment == null)
                {
                    _logger.LogWarning("🤔 Appointment not found with ID: {Id}", appointmentId);
                    return new NotFoundObjectResult($"No appointment found with ID: {appointmentId}");
                }

                // Store appointment details for response
                var clientName = $"{appointment.Client.FirstName} {appointment.Client.LastName}";
                var serviceName = appointment.Service.Name;
                var appointmentTime = appointment.Time;

                _logger.LogInformation("🗑️ Cancelling appointment: {ClientName} - {ServiceName} at {Time}", 
                    clientName, serviceName, appointmentTime);

                // Remove the appointment
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Appointment with ID {Id} cancelled successfully", appointmentId);
                
                return new OkObjectResult(new
                {
                    Message = "Appointment cancelled successfully",
                    Details = new
                    {
                        Id = appointmentId,
                        Client = clientName,
                        Service = serviceName,
                        Time = appointmentTime
                    }
                });
            }
            // Catch specific EF Core update exceptions
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "💥 Database update failed while cancelling appointment {Id}! Inner: {InnerMessage}", 
                    appointmentId, dbEx.InnerException?.Message);
                return new ObjectResult($"🔥 A database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}")
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
            // Catch any other unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while cancelling appointment {Id}!", appointmentId);
                return new ObjectResult("An unexpected error occurred while cancelling the appointment.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
