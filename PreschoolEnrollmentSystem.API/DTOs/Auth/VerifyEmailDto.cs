using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.API.DTOs.Auth
{
    public class VerifyEmailDto
    {
        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; } = string.Empty;
    }
}
