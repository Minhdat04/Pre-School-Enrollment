using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.API.DTOs.Auth
{
    public class PasswordResetDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
