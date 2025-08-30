using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Data;
using Microsoft.EntityFrameworkCore;
using API.Services;
using System;
using System.Threading.Tasks;

namespace API.Functions
{
    /// <summary>
    /// 💅 The Service Destroyer 💅
    /// Removes services from the beauty business (with safety checks).
    /// </summary>
    public class DeleteService
    {
        private readonly ILogger<DeleteService> _logger;
        private readonly ProjectContext _context;
        
        public DeleteService(ILogger<DeleteService> logger, ProjectContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 💅 The Careful Service Deletion Ritual 💅
        /// Azure Function triggered by HTTP DELETE to remove a service.
        /// </summary>
        [Function("DeleteService")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "manage/services/{serviceId:int}")] HttpRequest req,
            int serviceId)
        {
            _logger.LogInformation("💅 Delete service request received for service ID: {ServiceId}", serviceId);

            if (!AuthTokenService.ValidateRequest(req))
            {
                return new UnauthorizedResult();
            }
            
            try
            {
                // Check if service exists
                var service = await _context.Services
                    .Include(s => s.Category)
                    .FirstOrDefaultAsync(s => s.Id == serviceId);
                    
                if (service == null)
                {
                    return new NotFoundObjectResult($"Service with ID {serviceId} not found.");
                }

                // Check if service has any appointments
                var hasAppointments = await _context.Appointments
                    .AnyAsync(a => a.ServiceId == serviceId);
                    
                if (hasAppointments)
                {
                    return new BadRequestObjectResult("Cannot delete service: there are existing appointments for this service. Please cancel all appointments first or contact system administrator.");
                }

                _logger.LogInformation("🗑️ Deleting service: {ServiceName} (ID: {ServiceId})", service.Name, serviceId);
                
                // Delete the service
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Service deleted successfully: {ServiceId}", serviceId);
                
                return new OkObjectResult(new { 
                    message = $"Service '{service.Name}' has been successfully deleted.",
                    deletedServiceId = serviceId,
                    deletedServiceName = service.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred while deleting service!");
                return new ObjectResult("An unexpected error occurred while deleting the service.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
