using System;
using System.ComponentModel.DataAnnotations;

namespace PreschoolEnrollmentSystem.Core.DTOs.Auth
{
    public class VerifyEmailDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }
}
