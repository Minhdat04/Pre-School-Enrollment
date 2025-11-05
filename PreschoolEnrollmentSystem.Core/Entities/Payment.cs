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
    public class Payment : BaseEntity
    {
        [ForeignKey("MadeBy")]
        public Guid MadeById { get; set; }
        public virtual User MadeBy { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }
        public virtual Application Application { get; set; }

        public PaymentType Type { get; set; } // 'Payment' hoặc 'Refund'

        public string? vnp_TxnRef { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal vnp_Amount { get; set; } 

        public string? vnp_OrderInfo { get; set; }
        public string? vnp_TransactionNo { get; set; }
        public string? vnp_BankCode { get; set; }
        public string? vnp_CardType { get; set; }
        public string? vnp_PayDate { get; set; }
        public string? vnp_ResponseCode { get; set; }
        public string? vnp_TransactionStatus { get; set; }
    }
}
