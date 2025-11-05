using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsByUserAsync(Guid userId);
        Task<IEnumerable<Payment>> GetPaymentsByApplicationAsync(Guid applicationId);
        Task<Payment?> GetPaymentByTransactionRefAsync(string txnRef);
        Task<IEnumerable<Payment>> GetPaymentsByTypeAsync(PaymentType type);
        Task<Payment?> GetPaymentWithApplicationAsync(Guid paymentId);
        Task<Payment?> GetPaymentWithUserAsync(Guid paymentId);
        Task<decimal> GetTotalPaymentsByUserAsync(Guid userId);
    }
}
