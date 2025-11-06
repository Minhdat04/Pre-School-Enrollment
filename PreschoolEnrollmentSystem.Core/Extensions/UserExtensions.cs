using System;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Core.Extensions
{
    public static class UserExtensions
    {
        public static void CalculateProfileCompletion(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            int completedFields = 0;
            int totalFields = 0;

            // Common fields for all roles
            totalFields += 7; // FirebaseUid, FirstName, LastName, Email, Phone, Username, PasswordHash

            if (!string.IsNullOrWhiteSpace(user.FirebaseUid)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.FirstName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.LastName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.Email)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.Phone)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.Username)) completedFields++;
            if (!string.IsNullOrWhiteSpace(user.PasswordHash)) completedFields++;

            // Email verification
            totalFields++;
            if (user.EmailVerified) completedFields++;

            // Phone verification (if phone is provided)
            if (!string.IsNullOrWhiteSpace(user.Phone))
            {
                totalFields++;
                if (user.PhoneVerified) completedFields++;
            }

            // Role-specific fields
            if (user.Role == UserRole.Parent)
            {
                // Parents might need additional profile information
                // For now, just the basic fields
            }
            else if (user.Role == UserRole.Teacher)
            {
                // Teachers should have a classroom assigned
                totalFields++;
                if (user.ClassroomId.HasValue) completedFields++;
            }

            // Calculate percentage
            user.ProfileCompletionPercentage = totalFields > 0
                ? (int)Math.Round((double)completedFields / totalFields * 100)
                : 0;
        }

        public static void UpdateLastLogin(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.LastLoginAt = DateTime.UtcNow;
        }

        public static string GetFullName(this User user)
        {
            if (user == null)
                return string.Empty;

            return $"{user.FirstName} {user.LastName}".Trim();
        }

        public static bool CanEnroll(this User user)
        {
            if (user == null)
                return false;

            // Only parents can enroll children
            if (user.Role != UserRole.Parent)
                return false;

            // Must be active and have verified email
            return user.IsActive && user.EmailVerified && user.ProfileCompletionPercentage >= 80;
        }
    }
}
