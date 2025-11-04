using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    public class Classroom : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int Capacity { get; set; }

        public virtual ICollection<User> Teachers { get; set; } = new List<User>();

        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
