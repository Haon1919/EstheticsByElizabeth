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
    public class GetAppointmentHistoryByClient
    {
        private readonly ILogger<GetAppointmentHistoryByClient> _logger;
        private readonly ProjectContext _context;

        public GetAppointmentHistoryByClient(ILogger<GetAppointmentHistoryByClient> logger, ProjectContext context)
        {
            _logger = logger;
            _context = context;
        }

        [Function("GetAppointmentHistoryByClient")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("GetAppointmentHistoryByClient function processing request.");
            
            // Get the client email from the query string
            string email = req.Query["email"];
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email parameter provided.");
                return new BadRequestObjectResult("Please provide an email parameter.");
            }

            try
            {
                // Find the client by email
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.Email == email);
                
                if (client == null)
                {
                    _logger.LogWarning("Client not found with email: {Email}", email);
                    return new NotFoundObjectResult($"No client found with email: {email}");
                }

                // Get all appointments for this client
                var appointments = await _context.Appointments
                    .Include(a => a.Service)
                        .ThenInclude(s => s.Category)
                    .Where(a => a.ClientId == client.Id)
                    .OrderByDescending(a => a.Time)
                    .ToListAsync();
                
                _logger.LogInformation("Retrieved {Count} appointments for client with email {Email}", appointments.Count, email);
                
                return new OkObjectResult(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment history for client with email {Email}", email);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
