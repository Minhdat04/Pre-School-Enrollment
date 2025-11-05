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
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; } // Sẽ được hash, không lưu text

        public UserRole Role { get; set; } = UserRole.Parent;

        public UserStatus Status { get; set; } = UserStatus.Active;

        [ForeignKey("Classroom")]
        public Guid? ClassroomId { get; set; }
        public virtual Classroom? Classroom { get; set; }

        public virtual ICollection<Child> Children { get; set; } = new List<Child>();

        public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
