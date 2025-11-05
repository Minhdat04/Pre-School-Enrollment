using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Implementation
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly SmtpClient _smtpClient;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpClient = new SmtpClient
            {
                Host = _configuration["Email:SmtpHost"],
                Port = int.Parse(_configuration["Email:SmtpPort"]),
                EnableSsl = true,
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]
                )
            };
        }

        public async Task SendVerificationEmailAsync(string email, string verificationLink)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromEmail"]),
                Subject = "Verify Your Email",
                Body = $"<p>Click here to verify your email: <a href='{verificationLink}'>Verify Email</a></p>",
                IsBodyHtml = true
            };
            message.To.Add(email);

            await _smtpClient.SendMailAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromEmail"]),
                Subject = "Reset Your Password",
                Body = $"<p>Click here to reset your password: <a href='{resetLink}'>Reset Password</a></p>",
                IsBodyHtml = true
            };
            message.To.Add(email);

            await _smtpClient.SendMailAsync(message);
        }

        public async Task SendPasswordChangedNotificationAsync(string email)
        {
            var message = new MailMessage
            {
                From = new MailAddress(_configuration["Email:FromEmail"]),
                Subject = "Password Changed",
                Body = "<p>Your password has been changed successfully.</p>",
                IsBodyHtml = true
            };
            message.To.Add(email);

            await _smtpClient.SendMailAsync(message);
        }
    }
}
