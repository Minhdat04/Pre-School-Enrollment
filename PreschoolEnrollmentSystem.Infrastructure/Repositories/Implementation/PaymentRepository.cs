using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.MadeById == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByApplicationAsync(Guid applicationId)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.ApplicationId == applicationId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByTransactionRefAsync(string txnRef)
        {
            if (string.IsNullOrWhiteSpace(txnRef))
                return null;

            return await _dbSet
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(p => p.vnp_TxnRef == txnRef);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByTypeAsync(PaymentType type)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.Type == type)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentWithApplicationAsync(Guid paymentId)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.Application)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<Payment?> GetPaymentWithUserAsync(Guid paymentId)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.MadeBy)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<decimal> GetTotalPaymentsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted &&
                           p.MadeById == userId &&
                           p.Type == PaymentType.Payment)
                .SumAsync(p => p.vnp_Amount);
        }
    }
}
