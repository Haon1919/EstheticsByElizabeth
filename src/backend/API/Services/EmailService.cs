using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace API.Services
{
    /// <summary>
    /// 📧 Email Service Implementation 📧
    /// Currently logs emails to the console for development.
    /// Can be extended to use SendGrid, SMTP, or other email providers.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(EmailRequest emailRequest)
        {
            if (emailRequest == null)
                throw new ArgumentNullException(nameof(emailRequest));

            if (string.IsNullOrWhiteSpace(emailRequest.To))
                throw new ArgumentException("Email recipient is required", nameof(emailRequest));

            if (string.IsNullOrWhiteSpace(emailRequest.Subject))
                throw new ArgumentException("Email subject is required", nameof(emailRequest));

            // For development: Log the email instead of sending it
            _logger.LogInformation("📧 Email would be sent:");
            _logger.LogInformation("To: {To}", emailRequest.To);
            _logger.LogInformation("Subject: {Subject}", emailRequest.Subject);
            _logger.LogInformation("Body: {Body}", emailRequest.Body);
            _logger.LogInformation("IsHtml: {IsHtml}", emailRequest.IsHtml);

            // TODO: In production, implement actual email sending logic here
            // Examples:
            // - SendGrid API
            // - SMTP client
            // - Azure Communication Services
            // - AWS SES

            // Simulate async operation
            await Task.Delay(100);

            _logger.LogInformation("✅ Email processing completed for: {To}", emailRequest.To);
        }
    }
}
