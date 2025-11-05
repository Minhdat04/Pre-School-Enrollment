using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    using System;
    using System.Collections.Generic;
    using PreschoolEnrollmentSystem.Core.Enums;

    namespace PreschoolEnrollmentSystem.Core.Entities
    {
        public class Staff : BaseEntity
        {
            public string FirebaseUid { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool EmailVerified { get; set; } = false;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string FullName => $"{FirstName} {LastName}".Trim();
            public string PhoneNumber { get; set; } = string.Empty;
            public bool PhoneVerified { get; set; } = false;
            public string? EmployeeId { get; set; }
            public string JobTitle { get; set; } = string.Empty;
            public string? Department { get; set; }
            public DateTime HireDate { get; set; } = DateTime.UtcNow;
            public string EmploymentStatus { get; set; } = "Active";
            public string WorkScheduleType { get; set; } = "FullTime";
            public string? Qualifications { get; set; }
            public string BackgroundCheckStatus { get; set; } = "Pending";
            public DateTime? BackgroundCheckDate { get; set; }
            public DateTime? BackgroundCheckExpiresAt { get; set; }
            public UserRole Role { get; set; } = UserRole.Staff;
            public bool IsActive { get; set; } = true;
            public DateTime? LastLoginAt { get; set; }
            public string? EmergencyContactName { get; set; }
            public string? EmergencyContactPhone { get; set; }
            public string? EmergencyContactRelationship { get; set; }
            public string? Notes { get; set; }
            public string? ProfilePhotoUrl { get; set; }
            public string? Bio { get; set; }

            // Navigation Properties
            public virtual ICollection<Class> AssignedClasses { get; set; } = new List<Class>();

            // Methods
            public void UpdateLastLogin()
            {
                LastLoginAt = DateTime.UtcNow;
            }
            public bool IsEligibleToWorkWithChildren()
            {
                if (BackgroundCheckStatus != "Approved")
                    return false;

                if (BackgroundCheckExpiresAt.HasValue && BackgroundCheckExpiresAt.Value < DateTime.UtcNow)
                    return false;

                if (EmploymentStatus != "Active")
                    return false;

                if (!IsActive)
                    return false;

                return true;
            }
            public bool IsBackgroundCheckExpiringSoon(int daysThreshold = 30)
            {
                if (!BackgroundCheckExpiresAt.HasValue)
                    return false;

                var daysUntilExpiration = (BackgroundCheckExpiresAt.Value - DateTime.UtcNow).Days;
                return daysUntilExpiration <= daysThreshold && daysUntilExpiration > 0;
            }
            public int GetYearsOfService()
            {
                var timespan = DateTime.UtcNow - HireDate;
                return (int)(timespan.Days / 365.25); // Account for leap years
            }
            public bool HasAdminPrivileges()
            {
                return Role == UserRole.Admin;
            }
            public string GetDisplayNameWithTitle(string? title = null)
            {
                if (string.IsNullOrWhiteSpace(title))
                    return FullName;

                return $"{title} {FullName}";
            }
            public bool IsProfileComplete()
            {
                return !string.IsNullOrWhiteSpace(Email) &&
                       !string.IsNullOrWhiteSpace(FirstName) &&
                       !string.IsNullOrWhiteSpace(LastName) &&
                       !string.IsNullOrWhiteSpace(PhoneNumber) &&
                       !string.IsNullOrWhiteSpace(JobTitle) &&
                       !string.IsNullOrWhiteSpace(EmergencyContactName) &&
                       !string.IsNullOrWhiteSpace(EmergencyContactPhone) &&
                       EmailVerified &&
                       BackgroundCheckStatus == "Approved";
            }
        }
    }
}
