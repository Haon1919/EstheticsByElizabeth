using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class CancelAppointment
    {
        private readonly ILogger<CancelAppointment> _logger;
        private readonly ProjectContext _context;

        public CancelAppointment(ILogger<CancelAppointment> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("CancelAppointment")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequest req)
        {
            _logger.LogInformation("CancelAppointment function processing request.");
            
            // Get the appointment ID from the query string
            string idParam = req.Query["id"];
            
            if (string.IsNullOrEmpty(idParam) || !int.TryParse(idParam, out int id))
            {
                _logger.LogWarning("Invalid or missing appointment ID: {Id}", idParam);
                return new BadRequestObjectResult("Please provide a valid appointment ID.");
            }

            try
            {
                // Find the appointment by ID
                var appointment = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                    .FirstOrDefaultAsync(a => a.Id == id);
                
                if (appointment == null)
                {
                    _logger.LogWarning("Appointment not found with ID: {Id}", id);
                    return new NotFoundObjectResult($"No appointment found with ID: {id}");
                }

                // Store appointment details for response
                var clientName = $"{appointment.Client.FirstName} {appointment.Client.LastName}";
                var serviceName = appointment.Service.Name;
                var appointmentTime = appointment.Time;

                // Remove the appointment
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Appointment with ID {Id} cancelled successfully", id);
                
                return new OkObjectResult(new
                {
                    Message = $"Appointment cancelled successfully",
                    Details = new
                    {
                        Id = id,
                        Client = clientName,
                        Service = serviceName,
                        Time = appointmentTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment with ID {Id}", id);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
