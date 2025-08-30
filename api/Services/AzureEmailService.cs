using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure;

namespace API.Services
{
    /// <summary>
    /// 📧 Azure Communication Services Email Implementation 📧
    /// Free tier: 365 emails per month
    /// After free tier: $0.0025 per email
    /// </summary>
    public class AzureEmailService : IEmailService
    {
        private readonly ILogger<AzureEmailService> _logger;
        private readonly EmailClient _emailClient;
        private readonly string _fromAddress;

        public AzureEmailService(ILogger<AzureEmailService> logger, string connectionString, string fromAddress)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailClient = new EmailClient(connectionString);
            _fromAddress = fromAddress ?? throw new ArgumentNullException(nameof(fromAddress));
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
                _logger.LogInformation("📧 Sending email via Azure Communication Services to: {To}", emailRequest.To);

                var emailContent = new EmailContent(emailRequest.Subject);
                if (emailRequest.IsHtml)
                {
                    emailContent.Html = emailRequest.Body;
                }
                else
                {
                    emailContent.PlainText = emailRequest.Body;
                }

                var emailMessage = new EmailMessage(
                    senderAddress: _fromAddress,
                    content: emailContent,
                    recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(emailRequest.To) })
                );

                // Set reply-to if provided
                if (!string.IsNullOrWhiteSpace(emailRequest.ReplyTo))
                {
                    emailMessage.ReplyTo.Add(new EmailAddress(emailRequest.ReplyTo));
                }

                var response = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);

                _logger.LogInformation("✅ Email sent successfully via Azure Communication Services. Message ID: {MessageId}", response.Id);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "💥 Failed to send email via Azure Communication Services: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
                throw new EmailServiceException($"Failed to send email via Azure Communication Services: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error sending email via Azure Communication Services");
                throw new EmailServiceException($"Unexpected error sending email: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Custom exception for email service errors
    /// </summary>
    public class EmailServiceException : Exception
    {
        public EmailServiceException(string message) : base(message) { }
        public EmailServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
}
