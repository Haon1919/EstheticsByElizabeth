using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using API.Data;
using API.Services;
using API.Entities;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Functions
{
    public class AppointmentReminderTimer
    {
        private readonly ILogger<AppointmentReminderTimer> _logger;
        private readonly ProjectContext _context;
        private readonly IEmailService _emailService;

        public AppointmentReminderTimer(
            ILogger<AppointmentReminderTimer> logger,
            ProjectContext context,
            IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Azure Function that sends appointment reminder emails
        /// Converted from TimerTrigger to HttpTrigger for Azure Static Web Apps compatibility
        /// Originally: CRON expression "0 0 7 * * *" = run at 07:00 (7:00 AM) every day
        /// Now: Manual trigger via HTTP POST to /api/send-appointment-reminders
        /// </summary>
        [Function("AppointmentReminderTimer")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "send-appointment-reminders")] HttpRequest req)
        {
            _logger.LogInformation($"Appointment reminder timer function executed at: {DateTime.UtcNow}");

            try
            {
                // Get tomorrow's date
                var tomorrow = DateTime.Today.AddDays(1);
                var tomorrowEnd = tomorrow.AddDays(1);

                // Find all appointments for tomorrow with their associated services and clients
                var appointmentsTomorrow = await _context.Appointments
                    .Include(a => a.Service)
                    .Include(a => a.Client)
                    .Where(a => a.Time >= tomorrow && a.Time < tomorrowEnd)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} appointments for tomorrow ({Date:yyyy-MM-dd})", appointmentsTomorrow.Count, tomorrow);

                foreach (var appointment in appointmentsTomorrow)
                {
                    try
                    {
                        if (appointment.Client == null)
                        {
                            _logger.LogWarning($"Appointment {appointment.Id} has no associated client");
                            continue;
                        }

                        if (string.IsNullOrEmpty(appointment.Client.Email))
                        {
                            _logger.LogWarning($"Client {appointment.Client.Id} has no email address");
                            continue;
                        }

                        // Prepare email content
                        var subject = "Appointment Reminder - Esthetics by Elizabeth";
                        var emailBody = BuildReminderEmailBody(appointment);

                        // Send reminder email
                        var emailRequest = new API.Services.EmailRequest
                        {
                            To = appointment.Client.Email,
                            Subject = subject,
                            Body = emailBody,
                            IsHtml = true,
                            FromName = "Esthetics by Elizabeth"
                        };
                        
                        await _emailService.SendEmailAsync(emailRequest);

                        _logger.LogInformation("Sent reminder email to {Email} for appointment {AppointmentId}", appointment.Client.Email, appointment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send reminder email for appointment {appointment.Id}");
                    }
                }

                _logger.LogInformation($"Appointment reminder process completed. Processed {appointmentsTomorrow.Count} appointments");
                return new OkObjectResult(new { message = $"Successfully processed {appointmentsTomorrow.Count} appointment reminders", count = appointmentsTomorrow.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing appointment reminders");
                return new BadRequestObjectResult(new { error = "Failed to process appointment reminders", details = ex.Message });
            }
        }

        /// <summary>
        /// Builds the HTML email body for appointment reminders
        /// </summary>
        private string BuildReminderEmailBody(Appointment appointment)
        {
            var appointmentTime = appointment.Time.ToString("h:mm tt");
            var appointmentDate = appointment.Time.ToString("dddd, MMMM dd, yyyy");
            var serviceName = appointment.Service?.Name ?? "Your Service";
            var duration = appointment.Service?.Duration ?? 0;
            var aftercareInstructions = appointment.Service?.AfterCareInstructions;

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
            <h2>Appointment Reminder</h2>
            <p>Dear {appointment.Client.FirstName},</p>
            <p>This is a friendly reminder about your upcoming appointment with us!</p>
        </div>

        <div class='appointment-details'>
            <h3>Appointment Details:</h3>
            <ul>
                <li><strong>Service:</strong> {serviceName}</li>
                <li><strong>Date:</strong> {appointmentDate}</li>
                <li><strong>Time:</strong> <span class='highlight'>{appointmentTime}</span></li>
                <li><strong>Duration:</strong> {duration} minutes</li>
            </ul>
        </div>";

            // Add aftercare instructions if available
            if (!string.IsNullOrWhiteSpace(aftercareInstructions))
            {
                emailBody += $@"
        <div class='aftercare'>
            <h3>📋 Aftercare Instructions</h3>
            <p>Please review these important aftercare instructions for your {serviceName}:</p>
            <div style='white-space: pre-line; margin-top: 10px;'>
                {aftercareInstructions}
            </div>
        </div>";
            }

            emailBody += $@"
        <div>
            <h3>Important Reminders:</h3>
            <ul>
                <li>Please arrive 10-15 minutes early for your appointment</li>
                <li>If you need to reschedule or cancel, please contact us at least 24 hours in advance</li>
                <li>Bring any relevant medical history or skincare concerns to discuss</li>
            </ul>
        </div>

        <div>
            <p>We look forward to seeing you tomorrow! If you have any questions or need to make changes to your appointment, please don't hesitate to contact us.</p>
            <p><strong>Phone:</strong> (555) 123-4567<br>
            <strong>Email:</strong> info@estheticsbyelizabeth.com</p>
        </div>

        <div class='footer'>
            <p>Thank you for choosing Esthetics by Elizabeth!</p>
            <p><em>This is an automated reminder. Please do not reply to this email.</em></p>
        </div>
    </div>
</body>
</html>";

            return emailBody;
        }
    }
}