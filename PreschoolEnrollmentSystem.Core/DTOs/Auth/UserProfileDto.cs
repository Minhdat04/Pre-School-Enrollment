using System;

namespace PreschoolEnrollmentSystem.Core.DTOs.Auth
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? ProfileCompletionPercentage { get; set; }
    }
}
