using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendTestEmailAsync();
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var emailSettings = await GetEmailSettingsAsync();
                
                if (!ValidateEmailSettings(emailSettings))
                {
                    _logger.LogError("Email settings are not properly configured");
                    return false;
                }

                using var client = new SmtpClient(emailSettings.SmtpServer, emailSettings.Port)
                {
                    EnableSsl = emailSettings.UseSslTls,
                    Credentials = new NetworkCredential(emailSettings.Username, emailSettings.Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };

                mailMessage.To.Add(to);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", to);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync()
        {
            try
            {
                var emailSettings = await GetEmailSettingsAsync();
                
                if (!ValidateEmailSettings(emailSettings))
                {
                    return false;
                }

                var subject = "UmiHealth POS - Test Email";
                var body = @"
                    <h2>Test Email Successful</h2>
                    <p>This is a test email from UmiHealth POS system.</p>
                    <p><strong>Sent at:</strong> " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") + @"</p>
                    <p><strong>SMTP Server:</strong> " + emailSettings.SmtpServer + @"</p>
                    <hr>
                    <p><small>If you received this email, your email configuration is working correctly.</small></p>";

                return await SendEmailAsync(emailSettings.FromEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email");
                return false;
            }
        }

        private async Task<EmailSettings> GetEmailSettingsAsync()
        {
            var settings = await _context.AppSettings
                .Where(s => s.Category == "email")
                .ToListAsync();

            return new EmailSettings
            {
                SmtpServer = GetSettingValue(settings, "smtpServer") ?? "smtp.gmail.com",
                Port = int.Parse(GetSettingValue(settings, "port") ?? "587"),
                Username = GetSettingValue(settings, "username") ?? "",
                Password = GetSettingValue(settings, "password") ?? "",
                FromEmail = GetSettingValue(settings, "fromEmail") ?? "noreply@umihealth.com",
                FromName = GetSettingValue(settings, "fromName") ?? "UmiHealth POS",
                UseSslTls = bool.Parse(GetSettingValue(settings, "useSslTls") ?? "true")
            };
        }

        private string? GetSettingValue(List<AppSetting> settings, string key)
        {
            return settings.FirstOrDefault(s => s.Key == key)?.Value;
        }

        private bool ValidateEmailSettings(EmailSettings settings)
        {
            return !string.IsNullOrEmpty(settings.SmtpServer) &&
                   !string.IsNullOrEmpty(settings.Username) &&
                   !string.IsNullOrEmpty(settings.Password) &&
                   !string.IsNullOrEmpty(settings.FromEmail);
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "";
        public bool UseSslTls { get; set; } = true;
    }
}
