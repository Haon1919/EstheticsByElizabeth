using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace API.Services
{
    /// <summary>
    /// 📧 SMTP Email Service Implementation 📧
    /// Works with Outlook, Gmail, or any SMTP provider
    /// Free with personal accounts (with daily limits)
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpConfiguration _config;

        public SmtpEmailService(ILogger<SmtpEmailService> logger, SmtpConfiguration config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
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
                _logger.LogInformation("📧 Sending email via SMTP to: {To}", emailRequest.To);

                using var smtpClient = new SmtpClient(_config.Host, _config.Port)
                {
                    EnableSsl = _config.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_config.Username, _config.Password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_config.FromEmail, emailRequest.FromName ?? _config.FromName),
                    Subject = emailRequest.Subject,
                    Body = emailRequest.Body,
                    IsBodyHtml = emailRequest.IsHtml
                };

                mailMessage.To.Add(emailRequest.To);

                // Set reply-to if provided
                if (!string.IsNullOrWhiteSpace(emailRequest.ReplyTo))
                {
                    mailMessage.ReplyToList.Add(emailRequest.ReplyTo);
                }

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("✅ Email sent successfully via SMTP");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "💥 SMTP error sending email: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                throw new EmailServiceException($"SMTP error: {ex.StatusCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Unexpected error sending email via SMTP");
                throw new EmailServiceException($"Unexpected error sending email via SMTP: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// SMTP Configuration settings
    /// </summary>
    public class SmtpConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;

        /// <summary>
        /// Predefined configuration for Outlook/Hotmail
        /// </summary>
        public static SmtpConfiguration Outlook(string email, string password, string fromName = "Esthetics by Elizabeth")
        {
            return new SmtpConfiguration
            {
                Host = "smtp-mail.outlook.com",
                Port = 587,
                EnableSsl = true,
                Username = email,
                Password = password,
                FromEmail = email,
                FromName = fromName
            };
        }

        /// <summary>
        /// Predefined configuration for Gmail
        /// Note: Requires App Password, not your regular Gmail password
        /// </summary>
        public static SmtpConfiguration Gmail(string email, string appPassword, string fromName = "Esthetics by Elizabeth")
        {
            return new SmtpConfiguration
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Username = email,
                Password = appPassword,
                FromEmail = email,
                FromName = fromName
            };
        }
    }
}
