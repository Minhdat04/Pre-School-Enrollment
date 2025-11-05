using System;
using System.Collections.Generic;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    public class Parent : BaseEntity
    {
        public string FirebaseUid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; } = false;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string PhoneNumber { get; set; } = string.Empty;
        public bool PhoneVerified { get; set; } = false;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string Country { get; set; } = "United States";
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? RelationshipToChild { get; set; }
        public UserRole Role { get; set; } = UserRole.Parent;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public int ProfileCompletionPercentage { get; set; } = 0;
        public string PreferredContactMethod { get; set; } = "Email";
        public bool MarketingConsent { get; set; } = false;
        public bool AcceptedTerms { get; set; } = false;
        public DateTime? TermsAcceptedAt { get; set; }

        // Navigation Properties
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

        // Methods
        public int CalculateProfileCompletion()
        {
            int totalFields = 15; // Total number of profile fields
            int completedFields = 0;

            // Required fields
            if (!string.IsNullOrWhiteSpace(Email)) completedFields++;
            if (!string.IsNullOrWhiteSpace(FirstName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(LastName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(PhoneNumber)) completedFields++;

            // Address fields
            if (!string.IsNullOrWhiteSpace(AddressLine1)) completedFields++;
            if (!string.IsNullOrWhiteSpace(City)) completedFields++;
            if (!string.IsNullOrWhiteSpace(State)) completedFields++;
            if (!string.IsNullOrWhiteSpace(PostalCode)) completedFields++;
            if (!string.IsNullOrWhiteSpace(Country)) completedFields++;

            // Emergency contact
            if (!string.IsNullOrWhiteSpace(EmergencyContactName)) completedFields++;
            if (!string.IsNullOrWhiteSpace(EmergencyContactPhone)) completedFields++;
            if (!string.IsNullOrWhiteSpace(RelationshipToChild)) completedFields++;

            // Verification status
            if (EmailVerified) completedFields++;
            if (PhoneVerified) completedFields++;
            if (AcceptedTerms) completedFields++;

            ProfileCompletionPercentage = (int)Math.Round((double)completedFields / totalFields * 100);
            return ProfileCompletionPercentage;
        }
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }
        public bool IsProfileCompleteForEnrollment()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(FirstName) &&
                   !string.IsNullOrWhiteSpace(LastName) &&
                   !string.IsNullOrWhiteSpace(PhoneNumber) &&
                   !string.IsNullOrWhiteSpace(AddressLine1) &&
                   !string.IsNullOrWhiteSpace(City) &&
                   !string.IsNullOrWhiteSpace(State) &&
                   !string.IsNullOrWhiteSpace(PostalCode) &&
                   !string.IsNullOrWhiteSpace(EmergencyContactName) &&
                   !string.IsNullOrWhiteSpace(EmergencyContactPhone) &&
                   EmailVerified &&
                   AcceptedTerms;
        }
        public string GetFormattedAddress()
        {
            if (string.IsNullOrWhiteSpace(AddressLine1))
                return string.Empty;

            var address = AddressLine1;

            if (!string.IsNullOrWhiteSpace(AddressLine2))
                address += $", {AddressLine2}";

            if (!string.IsNullOrWhiteSpace(City))
                address += $", {City}";

            if (!string.IsNullOrWhiteSpace(State))
                address += $", {State}";

            if (!string.IsNullOrWhiteSpace(PostalCode))
                address += $" {PostalCode}";

            if (!string.IsNullOrWhiteSpace(Country))
                address += $", {Country}";

            return address;
        }
    }
}