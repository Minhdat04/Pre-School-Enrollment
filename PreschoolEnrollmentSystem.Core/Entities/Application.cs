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
    public class Application : BaseEntity
    {
        [ForeignKey("Child")]
        public Guid? ChildId { get; set; }
        public virtual Child? Child { get; set; }

        [Required]
        [MaxLength(100)]
        public string StudentName { get; set; }

        public DateTime Birthdate { get; set; }

        public Gender Gender { get; set; }

        [Required]
        [MaxLength(500)]
        public string Address { get; set; }

        [Required]
        [MaxLength(50)]
        public string Grade { get; set; } // Lớp (mầm, chồi, lá)

        public string? Reason { get; set; } // Lý do (nếu bị từ chối)

        public ApplicationStatus Status { get; set; } = ApplicationStatus.PaymentPending;

        // Mối quan hệ: Đơn này được tạo bởi ai
        [ForeignKey("CreatedBy")]
        public Guid CreatedById { get; set; }
        public virtual User CreatedBy { get; set; }

        // Mối quan hệ: Một đơn có nhiều thanh toán (cho phép hoàn tiền)
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
