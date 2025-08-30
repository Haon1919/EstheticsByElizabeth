using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace API.Services
{
    /// <summary>
    /// 📧 SendGrid Email Service Implementation 📧
    /// Free tier: 25,000 emails first month, then 100/day
    /// Perfect for startups and small businesses
    /// </summary>
    public class SendGridEmailService : IEmailService
    {
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly ISendGridClient _sendGridClient;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(ILogger<SendGridEmailService> logger, string apiKey, string fromEmail, string fromName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sendGridClient = new SendGridClient(apiKey);
            _fromEmail = fromEmail ?? throw new ArgumentNullException(nameof(fromEmail));
            _fromName = fromName ?? "Esthetics by Elizabeth";
        }

        public async Task SendEmailAsync(EmailRequest emailRequest)
        {
            if (emailRequest == null)
                throw new ArgumentNullException(nameof(emailRequest));

            if (string.IsNullOrWhiteSpace(emailRequest.To))
                throw new ArgumentException("Email recipient is required", nameof(emailRequest));

            if (string.IsNullOrWhiteSpace(emailRequest.Subject))
                throw new ArgumentException("Email subject is required", nameof(emailRequest));

            try
            {
                _logger.LogInformation("📧 Sending email via SendGrid to: {To}", emailRequest.To);

                var from = new EmailAddress(_fromEmail, emailRequest.FromName ?? _fromName);
                var to = new EmailAddress(emailRequest.To);
                
                SendGridMessage msg;
                
                if (emailRequest.IsHtml)
                {
                    msg = MailHelper.CreateSingleEmail(from, to, emailRequest.Subject, null, emailRequest.Body);
                }
                else
                {
                    msg = MailHelper.CreateSingleEmail(from, to, emailRequest.Subject, emailRequest.Body, null);
                }

                // Set reply-to if provided
                if (!string.IsNullOrWhiteSpace(emailRequest.ReplyTo))
                {
                    msg.SetReplyTo(new EmailAddress(emailRequest.ReplyTo));
                }

                // Add custom headers for tracking
                msg.AddCustomArg("source", "esthetics-app");
                msg.AddCustomArg("timestamp", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Email sent successfully via SendGrid. Status: {StatusCode}", response.StatusCode);
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("💥 SendGrid API error: Status {StatusCode}, Body: {ResponseBody}", response.StatusCode, responseBody);
                    throw new EmailServiceException($"SendGrid API error: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex) when (!(ex is EmailServiceException))
            {
                _logger.LogError(ex, "💥 Unexpected error sending email via SendGrid");
                throw new EmailServiceException($"Unexpected error sending email via SendGrid: {ex.Message}", ex);
            }
        }
    }
}
