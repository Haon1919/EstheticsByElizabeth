# 📧 Email Service Setup Guide

This guide explains how to configure and use the email services in your Esthetics by Elizabeth application.

## 🏗️ Available Email Providers

### 1. **Azure Communication Services** (Recommended for Azure hosting)
- **Free Tier**: 365 emails per month
- **Paid**: $0.0025 per email after free tier
- **Best for**: Azure-hosted applications, professional setup

### 2. **SendGrid**
- **Free Tier**: 25,000 emails first month, then 100/day
- **Best for**: High-volume applications, marketing emails

### 3. **SMTP** (Gmail/Outlook)
- **Free Tier**: 300 emails/day (Gmail), varies (Outlook)
- **Best for**: Small businesses, low-volume applications

### 4. **Development** (Console logging)
- **Free**: Always free
- **Best for**: Development and testing

## 🐳 Local Development Setup

### 1. Start Services with MailHog

```bash
cd /Users/noahweirdo/Projects/EstheticsByElizabeth/docker
./start-services.sh
```

This will start:
- PostgreSQL database
- MinIO object storage
- **MailHog email service** (SMTP server + Web UI)
- Your API

### 2. Access MailHog Web UI

Open http://localhost:8025 to view captured emails in a web interface.

### 3. Test Email Service

Send a test email:
```bash
curl -X POST "http://localhost/api/test/email?to=your-email@example.com"
```

Or test with the default email:
```bash
curl -X POST "http://localhost/api/test/email"
```

## ⚙️ Configuration

### Local Development (local.settings.json)

```json
{
  "Values": {
    "Email:Provider": "smtp",
    "Email:Smtp:Host": "localhost",
    "Email:Smtp:Port": "1025",
    "Email:Smtp:EnableSsl": "false",
    "Email:Smtp:Username": "test@estheticsbyelizabeth.com",
    "Email:Smtp:Password": "testpassword",
    "Email:Smtp:FromEmail": "noreply@estheticsbyelizabeth.com",
    "Email:Smtp:FromName": "Esthetics by Elizabeth"
  }
}
```

### Azure Communication Services

1. Create Azure Communication Services resource
2. Add Email Communication Service
3. Configure your domain
4. Update configuration:

```json
{
  "Values": {
    "Email:Provider": "azure",
    "Email:Azure:ConnectionString": "endpoint=https://your-acs.communication.azure.com/;accesskey=your-key",
    "Email:Azure:FromAddress": "DoNotReply@your-domain.azurecomm.net"
  }
}
```

### SendGrid

1. Sign up at https://sendgrid.com/
2. Create API Key
3. Verify sender identity
4. Update configuration:

```json
{
  "Values": {
    "Email:Provider": "sendgrid",
    "Email:SendGrid:ApiKey": "SG.your-api-key-here",
    "Email:SendGrid:FromEmail": "noreply@yourdomain.com",
    "Email:SendGrid:FromName": "Esthetics by Elizabeth"
  }
}
```

### Gmail SMTP

1. Enable 2-Factor Authentication
2. Generate App Password
3. Update configuration:

```json
{
  "Values": {
    "Email:Provider": "smtp",
    "Email:Smtp:Host": "smtp.gmail.com",
    "Email:Smtp:Port": "587",
    "Email:Smtp:EnableSsl": "true",
    "Email:Smtp:Username": "your-email@gmail.com",
    "Email:Smtp:Password": "your-app-password",
    "Email:Smtp:FromEmail": "your-email@gmail.com",
    "Email:Smtp:FromName": "Esthetics by Elizabeth"
  }
}
```

## 🧪 Testing Email Services

### 1. Test Email Endpoint

```bash
# Test with default email
curl -X POST "http://localhost/api/test/email"

# Test with specific email
curl -X POST "http://localhost/api/test/email?to=your-email@example.com"
```

### 2. Check Logs

Monitor the console output for email sending logs and any errors.

### 3. Verify with MailHog (Local Development)

1. Open http://localhost:8025
2. Send a test email
3. Check if it appears in the MailHog inbox

## 🚀 Production Deployment

### Azure App Service Environment Variables

Set these in your Azure App Service configuration:

```
Email__Provider=azure
Email__Azure__ConnectionString=your-connection-string
Email__Azure__FromAddress=DoNotReply@yourdomain.azurecomm.net
```

### Azure Functions Configuration

Update your Azure Functions application settings with the same variables.

## 📝 Using Email Service in Code

```csharp
public class YourFunction
{
    private readonly IEmailService _emailService;

    public YourFunction(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task SendEmail()
    {
        var emailRequest = new EmailRequest
        {
            To = "customer@example.com",
            Subject = "Appointment Confirmation",
            Body = "<h1>Your appointment is confirmed!</h1>",
            IsHtml = true,
            ReplyTo = "support@estheticsbyelizabeth.com"
        };

        await _emailService.SendEmailAsync(emailRequest);
    }
}
```

## 🛠️ Troubleshooting

### Email Not Sending

1. Check the logs for error messages
2. Verify configuration settings
3. Test with development provider first
4. Check email provider limits and quotas

### MailHog Not Working

1. Ensure Docker services are running: `docker ps`
2. Check MailHog logs: `docker logs esthetics-mailhog`
3. Verify port 8025 is not in use by another application

### SMTP Authentication Errors

1. For Gmail: Use App Password, not regular password
2. For Outlook: Ensure account supports SMTP
3. Check firewall settings for SMTP ports

### Azure Communication Services Errors

1. Verify connection string is correct
2. Check if domain is verified
3. Ensure from address matches configured domain

## 📊 Email Service Comparison

| Provider | Free Tier | Setup Difficulty | Best For |
|----------|-----------|------------------|----------|
| Azure Communication Services | 365/month | Easy (Azure users) | Production Azure apps |
| SendGrid | 25k first month, then 100/day | Medium | High-volume apps |
| Gmail SMTP | 300/day | Easy | Small businesses |
| Outlook SMTP | Varies | Easy | Small businesses |
| Development | Unlimited | Very Easy | Development only |

## 🔒 Security Best Practices

1. **Never commit API keys** to version control
2. **Use environment variables** for sensitive configuration
3. **Rotate API keys** regularly
4. **Monitor email usage** to detect abuse
5. **Implement rate limiting** for email endpoints
6. **Validate email addresses** before sending
7. **Use HTTPS** for all email-related endpoints

## 🔄 Next Steps

1. Set up your preferred email provider
2. Test email functionality with the test endpoint
3. Configure email templates for your application
4. Set up monitoring and alerting for email failures
5. Consider implementing email queuing for high-volume scenarios
