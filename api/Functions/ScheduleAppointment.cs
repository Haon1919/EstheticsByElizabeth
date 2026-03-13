using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.DTOs;
using API.Services;

namespace API.Functions
{
    public class ScheduleAppointment
    {
        private readonly ILogger<ScheduleAppointment> _logger;
        private readonly ProjectContext _context;
        private readonly IBookingService _bookingService;

        public ScheduleAppointment(ILogger<ScheduleAppointment> logger, ProjectContext context, IBookingService bookingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
        }

        [Function("ScheduleAppointment")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "appointments")] HttpRequest req)
        {
            _logger.LogInformation("✨ Appointment scheduling request received.");

            // Rate Limiting Check
            var ipAddress = req.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            else
            {
                ipAddress = ipAddress.Split(',')[0].Trim();
            }
            
            if (_bookingAttempts.TryGetValue(ipAddress, out var attemptState))
            {
                if (attemptState.Attempts >= MaxAttempts)
                {
                    if (DateTimeOffset.UtcNow < attemptState.LastAttempt.AddMinutes(LockoutMinutes))
                    {
                        _logger.LogWarning("🚫 Rate limit exceeded for IP: {IpAddress}", ipAddress);
                        return new ObjectResult(new { success = false, message = "Too many booking attempts. Please try again later." })
                        {
                            StatusCode = StatusCodes.Status429TooManyRequests
                        };
                    }
                    else
                    {
                        // Lockout expired, reset
                         _bookingAttempts.TryRemove(ipAddress, out _);
                    }
                }
            }

            // Increment attempt counter
            _bookingAttempts.AddOrUpdate(ipAddress, 
                (1, DateTimeOffset.UtcNow), 
                (key, oldState) => (oldState.Attempts + 1, DateTimeOffset.UtcNow));

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var appointmentDto = JsonSerializer.Deserialize<CreateAppointmentDto>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (appointmentDto?.Client == null || appointmentDto.ServiceId == 0 || appointmentDto.Time == default)
                {
                    _logger.LogWarning("🚫 Invalid or incomplete appointment data received.");
                    return new BadRequestObjectResult("Invalid or incomplete appointment data provided.");
                }

                if (!string.IsNullOrEmpty(appointmentDto.Client.Email))
                {
                    _logger.LogInformation("🚨 Checking if client is under review: {ClientEmail}", appointmentDto.Client.Email);
                    var clientReviewResult = await _bookingService.CheckClientReviewStatusAsync(appointmentDto.Client.Email, appointmentDto.Time.Date);
                    
                    if (clientReviewResult.IsUnderReview)
                    {
                        _logger.LogWarning("⛔ Client {ClientEmail} is currently under review and cannot book appointments", appointmentDto.Client.Email);
                        return new ConflictObjectResult("Your account is currently under review and cannot book appointments. Please contact the admin for assistance.");
                    }
                    
                    if (clientReviewResult.ExistingAppointmentsCount > 0)
                    {
                        _logger.LogWarning("⚠️ Client {ClientEmail} is attempting to book multiple appointments on {Date}", 
                            appointmentDto.Client.Email, appointmentDto.Time.ToString("yyyy-MM-dd"));
                        _logger.LogInformation("🔄 Allowing multiple booking but marking for admin review");
                    }
                }

                _logger.LogInformation("🔍 Checking existence of Service ID: {ServiceId}", appointmentDto.ServiceId);
                var service = await _context.Services
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == appointmentDto.ServiceId);
                if (service == null)
                {
                    _logger.LogWarning("🤔 Service with ID {ServiceId} not found.", appointmentDto.ServiceId);
                    return new NotFoundObjectResult($"Service with ID {appointmentDto.ServiceId} not found.");
                }

                _logger.LogInformation("⏰ Checking availability for time slot: {AppointmentTime}", appointmentDto.Time);
                bool isTimeAvailable = await _bookingService.CheckIfTimeIsAvailableAsync(appointmentDto, service);
                if (!isTimeAvailable)
                {
                    _logger.LogWarning("⏳ The time {AppointmentTime} is already booked.", appointmentDto.Time);
                    return new ConflictObjectResult($"The selected time ({appointmentDto.Time:yyyy-MM-dd HH:mm}) is already booked.");
                }
                _logger.LogInformation("✅ Time slot {AppointmentTime} appears to be available.", appointmentDto.Time);

                var result = await _bookingService.BookAppointmentStepsAsync(appointmentDto, service);
                return result;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "💥 Database update failed! Constraint violation or concurrency issue likely. Inner: {InnerMessage}", dbEx.InnerException?.Message);
                return new ObjectResult($"🔥 A database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}")
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
            catch (JsonException jsonEx)
            {
                 _logger.LogError(jsonEx, "💥 Could not deserialize request body!");
                 return new BadRequestObjectResult($"Invalid JSON format in request body: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred during appointment scheduling!");
                return new ObjectResult($"🔥 An unexpected error occurred: {ex.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}