using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string verificationLink);
        Task SendEmailVerificationAsync(string email, string verificationLink);
        Task SendPasswordResetEmailAsync(string email, string resetLink);
        Task SendWelcomeEmailAsync(string email, string firstName);
        Task SendPasswordChangedNotificationAsync(string email);
    }
}
