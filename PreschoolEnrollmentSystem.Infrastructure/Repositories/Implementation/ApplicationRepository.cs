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
    public class ApplicationRepository : Repository<Application>, IApplicationRepository
    {
        public ApplicationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Application>> GetApplicationsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted && a.CreatedById == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetApplicationsByStatusAsync(ApplicationStatus status)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted && a.Status == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Application>> GetPendingPaymentApplicationsAsync()
        {
            return await GetApplicationsByStatusAsync(ApplicationStatus.PaymentPending);
        }

        public async Task<IEnumerable<Application>> GetApprovedApplicationsAsync()
        {
            return await GetApplicationsByStatusAsync(ApplicationStatus.Approved);
        }

        public async Task<IEnumerable<Application>> GetRejectedApplicationsAsync()
        {
            return await GetApplicationsByStatusAsync(ApplicationStatus.Rejected);
        }

        public async Task<Application?> GetApplicationWithPaymentsAsync(Guid applicationId)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted)
                .Include(a => a.Payments.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(a => a.Id == applicationId);
        }

        public async Task<Application?> GetApplicationWithChildAsync(Guid applicationId)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted)
                .Include(a => a.Child)
                .FirstOrDefaultAsync(a => a.Id == applicationId);
        }

        public async Task<Application?> GetApplicationWithAllDetailsAsync(Guid applicationId)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted)
                .Include(a => a.Child)
                .Include(a => a.CreatedBy)
                .Include(a => a.Payments.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(a => a.Id == applicationId);
        }

        public async Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status)
        {
            return await _dbSet
                .Where(a => !a.IsDeleted && a.Status == status)
                .CountAsync();
        }

        public async Task<bool> HasPendingApplicationAsync(Guid userId)
        {
            return await _dbSet
                .AnyAsync(a => !a.IsDeleted &&
                              a.CreatedById == userId &&
                              a.Status == ApplicationStatus.PaymentPending);
        }
    }
}
