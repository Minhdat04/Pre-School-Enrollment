using System;

namespace PreschoolEnrollmentSystem.Core.DTOs.Auth
{
    public class FirebaseTokenInfo
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
