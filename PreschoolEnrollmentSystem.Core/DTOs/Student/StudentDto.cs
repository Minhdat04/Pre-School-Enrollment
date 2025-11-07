using PreschoolEnrollmentSystem.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.DTOs.Student
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public DateTime Birthdate { get; set; }
        public Gender Gender { get; set; }

        // Thông tin liên kết
        public Guid ParentId { get; set; }
        public string? ParentName { get; set; } 

        public Guid? ClassroomId { get; set; }
        public string? ClassroomName { get; set; } 

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
