using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using API.Data;
using API.DTOs;
using API.Entities;

namespace API.Services
{
    public interface IBookingService
    {
        Task<(bool IsUnderReview, int ExistingAppointmentsCount)> CheckClientReviewStatusAsync(string clientEmail, DateTimeOffset appointmentDate);
        Task<bool> CheckIfTimeIsAvailableAsync(CreateAppointmentDto appointmentDto, Service service);
        Task<IActionResult> BookAppointmentStepsAsync(CreateAppointmentDto appointmentDto, Service service);
    }

    public class BookingService : IBookingService
    {
        private readonly ILogger<BookingService> _logger;
        private readonly ProjectContext _context;
        private readonly IEmailService _emailService;

        public BookingService(ILogger<BookingService> logger, ProjectContext context, IEmailService emailService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        public async Task<bool> CheckIfTimeIsAvailableAsync(CreateAppointmentDto appointmentDto, Service service)
        {
            _logger.LogDebug("Checking time availability for {AppointmentTime}", appointmentDto.Time);

            const int defaultDuration = 60;

            bool timeSlotTaken = await _context.Appointments
                .AnyAsync(a => a.Time == appointmentDto.Time);

            if (service.Duration == null)
            {
                return !timeSlotTaken;
            }

            var proposedStartTime = appointmentDto.Time;
            var proposedEndTime = proposedStartTime.AddMinutes(service.Duration.Value);

            var timeSlotOverlaps = await _context.Appointments
                .Include(a => a.Service)
                .AnyAsync(existing =>
                    existing.Time < proposedEndTime &&
                    existing.Time.AddMinutes(existing.Service.Duration ?? defaultDuration) > proposedStartTime
                );

            if (timeSlotTaken || timeSlotOverlaps)
            {
                _logger.LogDebug("Time slot {AppointmentTime} is already booked.", appointmentDto.Time);
                return false;
            }

            return true;
        }

        public async Task<IActionResult> BookAppointmentStepsAsync(CreateAppointmentDto appointmentDto, Service service)
        {
            _logger.LogInformation("🚀 Starting booking steps for Service: {ServiceName}, Time: {AppointmentTime}, ClientEmail: {ClientEmail}",
                service.Name, appointmentDto.Time, appointmentDto.Client?.Email);

            Client? client;
            _logger.LogInformation("👤 Finding or creating client: {ClientEmail}", appointmentDto.Client?.Email);
            
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
            }

            _logger.LogInformation("📅 Creating appointment for Client ID: {ClientId} at {AppointmentTime}", client.Id, appointmentDto.Time);
            var appointment = new Appointment
            {
                ClientId = client.Id,
                ServiceId = appointmentDto.ServiceId,
                Time = appointmentDto.Time
            };
            _context.Appointments.Add(appointment);

            if (!string.IsNullOrEmpty(appointmentDto.Client!.Email))
            {
                var clientReviewResult = await CheckClientReviewStatusAsync(appointmentDto.Client.Email, appointmentDto.Time.Date);                
                if (clientReviewResult.ExistingAppointmentsCount > 0)
                {
                    await _context.SaveChangesAsync();
                    await FlagClientForReviewAsync(client.Id, appointmentDto.Time.Date, clientReviewResult.ExistingAppointmentsCount, appointment.Id);
                    _logger.LogInformation("✅ Appointment created with ID: {AppointmentId} and flagged for review", appointment.Id);
                }
                else
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Appointment created with ID: {AppointmentId}", appointment.Id);
                }
            }
            else
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Appointment created with ID: {AppointmentId}", appointment.Id);
            }

            _logger.LogInformation("🎉 Successfully scheduled appointment ID {AppointmentId} for client {ClientEmail}", appointment.Id, client.Email);

            try
            {
                _logger.LogInformation("📧 Sending confirmation email to: {ClientEmail}", client.Email);
                await SendConfirmationEmailAsync(appointment, client, service);
                _logger.LogInformation("✅ Confirmation email sent successfully to: {ClientEmail}", client.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "📧 Failed to send confirmation email to {ClientEmail} for appointment {AppointmentId}", 
                    client.Email, appointment.Id);
            }

            return new CreatedResult($"/api/appointments/{appointment.Id}", new
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

        public async Task<(bool IsUnderReview, int ExistingAppointmentsCount)> CheckClientReviewStatusAsync(string clientEmail, DateTimeOffset appointmentDate)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.ReviewFlags)
                    .FirstOrDefaultAsync(c => c.Email == clientEmail);

                if (client == null)
                {
                    _logger.LogInformation("👀 No client found with email {ClientEmail} during review check", clientEmail);
                    return (false, 0);
                }

                var activeReviewFlag = client.ReviewFlags
                    .Where(rf => rf.Status.ToUpper() == "PENDING" || rf.Status.ToUpper() == "REJECTED" || rf.Status.ToUpper() == "BANNED")
                    .OrderByDescending(rf => rf.FlagDate)
                    .FirstOrDefault();
                
                if (activeReviewFlag != null)
                {
                    _logger.LogWarning("⛔ Client {ClientEmail} has an active review flag with status {Status}", clientEmail, activeReviewFlag.Status);
                    return (true, 0);
                }

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
                return (false, 0);
            }
        }

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
                _logger.LogError(ex, "💥 Error flagging client ID {ClientId} for review", clientId);
            }
        }

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