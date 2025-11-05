using PreschoolEnrollmentSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    public class User : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string FirebaseUid { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        public bool EmailVerified { get; set; } = false;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool PhoneVerified { get; set; } = false;

        [Required]
        public string PasswordHash { get; set; }

        public UserRole Role { get; set; } = UserRole.Parent;

        public UserStatus Status { get; set; } = UserStatus.Active;

        public bool IsActive { get; set; } = true;

        public bool AcceptedTerms { get; set; } = false;

        public DateTime? TermsAcceptedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? CreatedBy { get; set; }

        public int ProfileCompletionPercentage { get; set; } = 0;

        [ForeignKey("Classroom")]
        public Guid? ClassroomId { get; set; }
        public virtual Classroom? Classroom { get; set; }

        public virtual ICollection<Child> Children { get; set; } = new List<Child>();

        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
