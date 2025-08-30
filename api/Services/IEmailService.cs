using System.Threading.Tasks;

namespace API.Services
{
    /// <summary>
    /// 📧 Email Service Interface 📧
    /// Defines the contract for sending emails.
    /// </summary>
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest emailRequest);
    }

    /// <summary>
    /// 📧 Email Request Model 📧
    /// Represents an email to be sent.
    /// </summary>
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = false;
        public string? FromName { get; set; }
        public string? ReplyTo { get; set; }
    }
}
