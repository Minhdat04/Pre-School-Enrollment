using System;

namespace PreschoolEnrollmentSystem.Core.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public Guid UserId { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? ProfileCompletionPercentage { get; set; }
        public bool CanEnroll { get; set; }
    }
}
