using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Text.RegularExpressions;

namespace SchoolSystem.Services
{
    public class EmailService : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IConfiguration configuration)
        {
            _smtpSettings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(_smtpSettings.Server) ||
                _smtpSettings.Port <= 0 ||
                string.IsNullOrEmpty(_smtpSettings.Username) ||
                string.IsNullOrEmpty(_smtpSettings.Password))
            {
                throw new ArgumentNullException("SMTP settings cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(toEmail))
            {
                throw new ArgumentNullException("Recipient email cannot be null or empty.");
            }

            if (!Regex.IsMatch(toEmail, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                throw new ArgumentException("Invalid email format", nameof(toEmail));
            }

            var mailMessage = CreateMailMessage(toEmail, subject, message);
            await SendEmailAsync(mailMessage);
        }

        private MailMessage CreateMailMessage(string toEmail, string subject, string message)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.Username),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            return mailMessage;
        }

        private async Task SendEmailAsync(MailMessage mailMessage)
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine("Email sent successfully!");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SMTP error: {ex.Message}");
                throw; // Optionally re-throw to handle it upstream
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; // Optionally re-throw to handle it upstream
            }
        }
    }
    // Create a strongly typed class for SMTP settings
    public class SmtpSettings
    {
        public string Server { get; set; }
        public int Port { get; set; } // Ensure this is an int
        public bool EnableSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
