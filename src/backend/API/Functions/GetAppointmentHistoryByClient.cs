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
    /// 📚 The Client History Librarian 📚
    /// Retrieves appointment history for specific clients.
    /// </summary>
    public class GetAppointmentHistoryByClient
    {
        private readonly ILogger<GetAppointmentHistoryByClient> _logger;
        private readonly ProjectContext _context;        public GetAppointmentHistoryByClient(ILogger<GetAppointmentHistoryByClient> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 📚 The Magical History Retrieval Ritual 📚
        /// Azure Function triggered by HTTP GET to retrieve client appointment history.
        /// </summary>        
        [Function("GetAppointmentHistoryByClient")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "appointments/history")] HttpRequest req)        {
            _logger.LogInformation("📚 Client appointment history request received.");

            // Get the client email from the query string
            string email = req.Query["email"];
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("🚫 No email parameter provided.");
                return new BadRequestObjectResult("Please provide an email parameter.");
            }

            try
            {
                _logger.LogInformation("🔍 Searching for client with email: {Email}", email);
                
                // Find the client by email
                var client = await _context.Clients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Email == email);
                
                if (client == null)
                {
                    _logger.LogWarning("🤔 Client not found with email: {Email}", email);
                    return new NotFoundObjectResult($"No client found with email: {email}");
                }                _logger.LogInformation("📋 Retrieving appointment history for client: {ClientName} ({Email})", 
                    $"{client.FirstName} {client.LastName}", email);                // Get all appointments for this client
                var appointments = await _context.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Service)
                        .ThenInclude(s => s.Category)
                    .Where(a => a.ClientId == client.Id)
                    .OrderByDescending(a => a.Time)
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
                            a.Service.AppointmentBufferTime,
                            Category = new
                            {
                                a.Service.Category.Id,
                                a.Service.Category.Name
                            }
                        }
                    })
                    .ToListAsync();
                
                _logger.LogInformation("✅ Retrieved {Count} appointments for client with email {Email}", 
                    appointments.Count, email);                return new OkObjectResult(new 
                {
                    Client = new
                    {
                        client.Id,
                        client.FirstName,
                        client.LastName,
                        client.Email,
                        client.PhoneNumber
                    },
                    Appointments = appointments,
                    TotalCount = appointments.Count
                });
            }
            // Catch any unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while retrieving appointment history for email {Email}!", email);
                return new ObjectResult("An unexpected error occurred while retrieving appointment history.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
