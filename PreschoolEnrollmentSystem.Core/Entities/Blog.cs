using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Core.Entities
{
    public class Blog : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Image { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        [MaxLength(100)]
        public string Author { get; set; }

        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
