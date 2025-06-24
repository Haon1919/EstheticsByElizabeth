using System;
using System.IO;
using System.Linq; // Needed for LINQ methods like Where, AnyAsync
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; // Needed for IActionResult types
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging; // Needed for ILogger
using Microsoft.EntityFrameworkCore; // Needed for EF Core operations (DbContext, DbUpdateException, etc.)
using API.Data; // Your DbContext namespace
using API.DTOs; // Your DTOs namespace
using API.Entities;
using API.Services; // Add this for IEmailService
using Npgsql; // Your Entities namespace

// Removed Polly using statement

namespace API.Functions
{
    /// <summary>
    /// 📅 The Appointment Wizard 📅
    /// Handles scheduling new appointments.
    /// </summary>
    public class ScheduleAppointment
    {
        private readonly ILogger<ScheduleAppointment> _logger;
        private readonly ProjectContext _context;
        private readonly IEmailService _emailService; // Add email service

        public ScheduleAppointment(ILogger<ScheduleAppointment> logger, ProjectContext context, IEmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService)); // Initialize email service
        }        /// <summary>
        /// 🧙‍♂️ The Magical Appointment Scheduling Ritual 🧙‍♂️
        /// Azure Function triggered by HTTP POST to schedule an appointment.
        /// </summary>        
        [Function("ScheduleAppointment")]        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "appointments")] HttpRequest req)        {
            _logger.LogInformation("✨ Appointment scheduling request received.");

            try
            {
                // 📜 Decode the incoming request body for appointment details
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var appointmentDto = JsonSerializer.Deserialize<CreateAppointmentDto>(
                    requestBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (appointmentDto?.Client == null || appointmentDto.ServiceId == 0 || appointmentDto.Time == default)
                {
                    _logger.LogWarning("🚫 Invalid or incomplete appointment data received.");
                    return new BadRequestObjectResult("Invalid or incomplete appointment data provided.");
                }

                // --- Client Review Status Check (First) ---
                if (!string.IsNullOrEmpty(appointmentDto.Client.Email))
                {
                    _logger.LogInformation("🚨 Checking if client is under review: {ClientEmail}", appointmentDto.Client.Email);
                    var clientReviewResult = await CheckClientReviewStatusAsync(appointmentDto.Client.Email, appointmentDto.Time.Date);
                    
                    if (clientReviewResult.IsUnderReview)
                    {
                        _logger.LogWarning("⛔ Client {ClientEmail} is currently under review and cannot book appointments", appointmentDto.Client.Email);
                        return new ConflictObjectResult("Your account is currently under review and cannot book appointments. Please contact the admin for assistance.");
                    }
                    
                    if (clientReviewResult.ExistingAppointmentsCount > 0)
                    {
                        _logger.LogWarning("⚠️ Client {ClientEmail} is attempting to book multiple appointments on {Date}", 
                            appointmentDto.Client.Email, appointmentDto.Time.ToString("yyyy-MM-dd"));
                        
                        // We'll allow the booking but flag it for review
                        _logger.LogInformation("🔄 Allowing multiple booking but marking for admin review");
                    }
                }

                // --- Service Validation ---
                _logger.LogInformation("🔍 Checking existence of Service ID: {ServiceId}", appointmentDto.ServiceId);
                var service = await _context.Services
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == appointmentDto.ServiceId);
                if (service == null)
                {
                    _logger.LogWarning("🤔 Service with ID {ServiceId} not found.", appointmentDto.ServiceId);
                    return new NotFoundObjectResult($"Service with ID {appointmentDto.ServiceId} not found.");
                }

                // --- Time Availability Check ---
                _logger.LogInformation("⏰ Checking availability for time slot: {AppointmentTime}", appointmentDto.Time);
                bool isTimeAvailable = await CheckIfTimeIsAvailableAsync(appointmentDto, service);
                if (!isTimeAvailable)
                {
                    _logger.LogWarning("⏳ The time {AppointmentTime} is already booked.", appointmentDto.Time);
                    return new ConflictObjectResult($"The selected time ({appointmentDto.Time:yyyy-MM-dd HH:mm}) is already booked.");
                }
                _logger.LogInformation("✅ Time slot {AppointmentTime} appears to be available.", appointmentDto.Time);                // Duplicate check removed - admin will handle any abuse cases via whitelist/manual review

                // --- Perform Booking Steps ---
                var result = await BookAppointmentStepsAsync(appointmentDto, service);
                return result;
            }
            // Catch specific EF Core update exceptions (e.g., constraint violations)
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "💥 Database update failed! Constraint violation or concurrency issue likely. Inner: {InnerMessage}", dbEx.InnerException?.Message);
                return new ObjectResult($"🔥 A database error occurred: {dbEx.InnerException?.Message ?? dbEx.Message}")
                {
                    // 409 Conflict is often suitable for DbUpdateExceptions
                    StatusCode = StatusCodes.Status409Conflict
                };
            }
             //Catch errors during JSON deserialization
            catch (JsonException jsonEx)
            {
                 _logger.LogError(jsonEx, "💥 Could not deserialize request body!");
                 return new BadRequestObjectResult($"Invalid JSON format in request body: {jsonEx.Message}");
            }
            // Catch any other unexpected exceptions
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 An unexpected error occurred during appointment scheduling!");

                // Generic error for other cases
                return new ObjectResult($"🔥 An unexpected error occurred: {ex.Message}")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }        }        /// <summary>
        /// ⏰ Checks if the requested time slot is available (Optimized).
        /// </summary>
        private async Task<bool> CheckIfTimeIsAvailableAsync(CreateAppointmentDto appointmentDto, Service service)
        {
            _logger.LogDebug("Checking time availability for {AppointmentTime}", appointmentDto.Time);

            bool timeSlotTaken = await _context.Appointments
                .AnyAsync(a => a.Time == appointmentDto.Time);
            
            // Skip overlap check if service duration is null
            if (service.Duration == null)
            {
                return !timeSlotTaken;
            }            var proposedStartTime = appointmentDto.Time;
            var proposedEndTime = proposedStartTime.AddMinutes(service.Duration.Value); // Duration is in minutes

            var timeSlotOverlaps = await _context.Appointments
                .AnyAsync(existing =>
                    existing.Time < proposedEndTime &&
                    existing.Time.AddHours(1) > proposedStartTime && 
                    existing.ServiceId == appointmentDto.ServiceId 
                );
            
            if(timeSlotTaken || timeSlotOverlaps)
            {
                _logger.LogDebug("Time slot {AppointmentTime} is already booked.", appointmentDto.Time);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 📝 Handles the steps of finding/creating a client and then creating the appointment.
        /// Performs separate SaveChangesAsync calls (separate implicit transactions).
        /// </summary>
        private async Task<IActionResult> BookAppointmentStepsAsync(
            CreateAppointmentDto appointmentDto,
            Service service)
        {            _logger.LogInformation("🚀 Starting booking steps for Service: {ServiceName}, Time: {AppointmentTime}, ClientEmail: {ClientEmail}",
                service.Name, appointmentDto.Time, appointmentDto.Client?.Email);

            Client? client;            // --- Step 1: Find or Create Client ---
            _logger.LogInformation("👤 Finding or creating client: {ClientEmail}", appointmentDto.Client?.Email);
            // Use EF Core directly; resilience is handled by EnableRetryOnFailure in Program.cs
            client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Email == appointmentDto.Client!.Email);            
            
            if (client == null)
            {
                _logger.LogInformation("✨ Client not found. Creating new client: {ClientEmail}", appointmentDto.Client!.Email);
                client = new Client
                {
                    FirstName = appointmentDto.Client.FirstName,
                    LastName = appointmentDto.Client.LastName,
                    Email = appointmentDto.Client.Email,
                    PhoneNumber = appointmentDto.Client.PhoneNumber
                };
                _context.Clients.Add(client);

                int clientChanges = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Client created with ID: {ClientId}. Rows affected: {RowsAffected}", client.Id, clientChanges);
            }
            else
            {
                _logger.LogInformation("👋 Found existing client: {ClientEmail} (ID: {ClientId})", client.Email, client.Id);
            }            // --- Step 2: Create the Appointment ---
            _logger.LogInformation("📅 Creating appointment for Client ID: {ClientId} at {AppointmentTime}", client.Id, appointmentDto.Time);
            var appointment = new Appointment
            {
                ClientId = client.Id,
                ServiceId = appointmentDto.ServiceId,
                Time = appointmentDto.Time
            };
            _context.Appointments.Add(appointment);            // Check if we need to flag this client for review (multiple bookings on same day)
            if (!string.IsNullOrEmpty(appointmentDto.Client!.Email))
            {
                var clientReviewResult = await CheckClientReviewStatusAsync(appointmentDto.Client.Email, appointmentDto.Time.Date);                
                if (clientReviewResult.ExistingAppointmentsCount > 0)
                {
                    // Save the appointment first to get its ID
                    await _context.SaveChangesAsync();
                    
                    // Now flag the client with the appointment ID
                    await FlagClientForReviewAsync(client.Id, appointmentDto.Time.Date, clientReviewResult.ExistingAppointmentsCount, appointment.Id);
                    
                    _logger.LogInformation("✅ Appointment created with ID: {AppointmentId} and flagged for review", appointment.Id);
                }
                else
                {
                    // Save the appointment normally
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Appointment created with ID: {AppointmentId}", appointment.Id);
                }
            }
            else
            {
                // Save the appointment if there's no client email to check
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Appointment created with ID: {AppointmentId}", appointment.Id);            }            _logger.LogInformation("🎉 Successfully scheduled appointment ID {AppointmentId} for client {ClientEmail}", appointment.Id, client.Email);

            // --- Send Confirmation Email ---
            try
            {
                _logger.LogInformation("📧 Sending confirmation email to: {ClientEmail}", client.Email);
                await SendConfirmationEmailAsync(appointment, client, service);
                _logger.LogInformation("✅ Confirmation email sent successfully to: {ClientEmail}", client.Email);
            }
            catch (Exception emailEx)
            {
                // Log the error but don't fail the appointment creation
                _logger.LogError(emailEx, "📧 Failed to send confirmation email to {ClientEmail} for appointment {AppointmentId}", 
                    client.Email, appointment.Id);
                // Email failure shouldn't prevent appointment from being created
            }

            // --- Return Success Response ---
            // Return a 201 Created status with the location of the new resource and its details
            return new CreatedResult($"/api/appointments/{appointment.Id}", new // Location header value
            {
                Id = appointment.Id,
                Time = appointment.Time,
                Service = new { Id = service.Id, Name = service.Name },
                Client = new
                {
                    Id = client.Id,
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Email = client.Email,
                    PhoneNumber = client.PhoneNumber
                }
            });
        }

        /// <summary>
        /// 🔍 Checks if a client is under review or if they're attempting to make multiple bookings on the same day.
        /// </summary>
        /// <param name="clientEmail">The client's email address</param>
        /// <param name="appointmentDate">The date of the requested appointment</param>
        /// <returns>Result containing review status and the count of existing appointments</returns>        /// <summary>
        /// Checks if a client is under review or has existing appointments on the specified date.
        /// </summary>
        /// <param name="clientEmail">The client's email address</param>
        /// <param name="appointmentDate">The date of the appointment</param>
        /// <returns>A tuple containing whether the client is under review and the count of existing appointments on the specified date</returns>
        private async Task<(bool IsUnderReview, int ExistingAppointmentsCount)> CheckClientReviewStatusAsync(
            string clientEmail, 
            DateTimeOffset appointmentDate)
        {
            try
            {
                // Find the client
                var client = await _context.Clients
                    .Include(c => c.ReviewFlags)
                    .FirstOrDefaultAsync(c => c.Email == clientEmail);

                if (client == null)
                {
                    _logger.LogInformation("👀 No client found with email {ClientEmail} during review check", clientEmail);
                    return (false, 0);
                }                // Check if client is under review
                var activeReviewFlag = client.ReviewFlags
                    .Where(rf => rf.Status.ToUpper() == "PENDING" || rf.Status.ToUpper() == "REJECTED" || rf.Status.ToUpper() == "BANNED")
                    .OrderByDescending(rf => rf.FlagDate)
                    .FirstOrDefault();
                
                if (activeReviewFlag != null)
                {
                    _logger.LogWarning("⛔ Client {ClientEmail} has an active review flag with status {Status}", clientEmail, activeReviewFlag.Status);
                    return (true, 0);
                }

                // Check for multiple bookings on the same day
                // Get the start and end of the appointment date
                var startDate = appointmentDate.Date;
                var endDate = startDate.AddDays(1);

                var existingAppointments = await _context.Appointments
                    .CountAsync(a => a.Client.Email == clientEmail && 
                                    a.Time >= startDate && 
                                    a.Time < endDate);
                
                if (existingAppointments > 0)
                {
                    _logger.LogWarning("⚠️ Client {ClientEmail} already has {Count} appointment(s) on {Date}", 
                        clientEmail, existingAppointments, appointmentDate.ToString("yyyy-MM-dd"));
                }

                return (false, existingAppointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error checking client review status for {ClientEmail}", clientEmail);
                // In case of error, we allow the booking to proceed
                return (false, 0);
            }
        }/// <summary>
        /// 🚩 Flags a client for review when they attempt to book multiple appointments on the same day.
        /// </summary>
        private async Task FlagClientForReviewAsync(int clientId, DateTimeOffset appointmentDate, int appointmentCount, int appointmentId)
        {
            try
            {
                var reviewFlag = new ClientReviewFlag
                {
                    ClientId = clientId,
                    AppointmentId = appointmentId,
                    FlagReason = $"Multiple bookings detected: Client attempted to book {appointmentCount + 1} appointments on {appointmentDate:yyyy-MM-dd}",
                    FlagDate = DateTimeOffset.UtcNow,
                    Status = "Pending",
                    AutoFlags = 1
                };

                _context.ClientReviewFlags.Add(reviewFlag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("🚩 Client ID {ClientId} flagged for review due to multiple bookings on {Date}", 
                    clientId, appointmentDate.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error flagging client ID {ClientId} for review", clientId);                // We don't throw here to avoid disrupting the appointment booking process
            }
        }

        /// <summary>
        /// 📧 Sends a confirmation email to the client after successful appointment booking
        /// </summary>
        private async Task SendConfirmationEmailAsync(Appointment appointment, Client client, Service service)
        {
            var appointmentTime = appointment.Time.ToString("h:mm tt");
            var appointmentDate = appointment.Time.ToString("dddd, MMMM dd, yyyy");
            var serviceName = service.Name;
            var duration = service.Duration ?? 0;
            var aftercareInstructions = service.AfterCareInstructions;

            var subject = "Appointment Confirmation - Esthetics by Elizabeth";
            var emailBody = BuildConfirmationEmailBody(appointment, client, service, appointmentDate, appointmentTime, serviceName, duration, aftercareInstructions);

            var emailRequest = new EmailRequest
            {
                To = client.Email,
                Subject = subject,
                Body = emailBody,
                IsHtml = true,
                FromName = "Esthetics by Elizabeth"
            };

            await _emailService.SendEmailAsync(emailRequest);
        }

        /// <summary>
        /// 🎨 Builds the HTML email body for appointment confirmation
        /// </summary>
        private string BuildConfirmationEmailBody(Appointment appointment, Client client, Service service, 
            string appointmentDate, string appointmentTime, string serviceName, int duration, string? aftercareInstructions)
        {
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 20px; }}
        .appointment-details {{ background-color: #e9ecef; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .aftercare {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
        .logo {{ text-align: center; margin-bottom: 20px; }}
        .confirmation-badge {{ background-color: #28a745; color: white; padding: 0.5rem 1rem; border-radius: 20px; display: inline-block; margin-bottom: 1rem; }}
        h1 {{ color: #2c3e50; }}
        h2 {{ color: #34495e; }}
        .highlight {{ font-weight: bold; color: #e74c3c; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>
            <h1>Esthetics by Elizabeth</h1>
        </div>
        
        <div class='header'>
            <div class='confirmation-badge'>✅ CONFIRMED</div>
            <h2>Your Appointment is Confirmed!</h2>
            <p>Dear {client.FirstName},</p>
            <p>Thank you for choosing Esthetics by Elizabeth! We're excited to provide you with exceptional service.</p>
        </div>

        <div class='appointment-details'>
            <h3>📅 Appointment Details:</h3>
            <ul>
                <li><strong>Service:</strong> {serviceName}</li>
                <li><strong>Date:</strong> {appointmentDate}</li>
                <li><strong>Time:</strong> <span class='highlight'>{appointmentTime}</span></li>
                <li><strong>Duration:</strong> {duration} minutes</li>
                <li><strong>Appointment ID:</strong> #{appointment.Id}</li>
            </ul>
        </div>";

            // Add aftercare instructions if available
            if (!string.IsNullOrWhiteSpace(aftercareInstructions))
            {
                emailBody += $@"
        <div class='aftercare'>
            <h3>📋 Pre-Appointment & Aftercare Instructions</h3>
            <p>Please review these important instructions for your {serviceName}:</p>
            <div style='white-space: pre-line; margin-top: 10px;'>
                {aftercareInstructions}
            </div>
        </div>";
            }

            emailBody += $@"
        <div>
            <h3>Important Information:</h3>
            <ul>
                <li>Please arrive 10-15 minutes early for your appointment</li>
                <li>If you need to reschedule or cancel, please contact us at least 24 hours in advance</li>
                <li>Bring any relevant medical history or skincare concerns to discuss</li>
                <li>Feel free to contact us if you have any questions about your upcoming service</li>
            </ul>
        </div>

        <div>
            <h3>Contact Information:</h3>
            <p><strong>Phone:</strong> (555) 123-4567<br>
            <strong>Email:</strong> info@estheticsbyelizabeth.com<br>
            <strong>Address:</strong> [Your Business Address]</p>
        </div>

        <div>
            <p>We look forward to seeing you on {appointmentDate}! Thank you for trusting us with your skincare needs.</p>
        </div>

        <div class='footer'>
            <p>Thank you for choosing Esthetics by Elizabeth!</p>
            <p><em>This is an automated confirmation email. Please save this email for your records.</em></p>
        </div>
    </div>
</body>
</html>";

            return emailBody;
        }
    }
}