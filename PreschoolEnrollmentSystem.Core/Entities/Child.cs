using PreschoolEnrollmentSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    public class Child : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        public DateTime Birthdate { get; set; }

        public Gender Gender { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        public string? Image { get; set; }

        public string? BirthCertificateImage { get; set; }

        [ForeignKey("Parent")]
        public Guid ParentId { get; set; }
        public virtual User Parent { get; set; }
    }
}
