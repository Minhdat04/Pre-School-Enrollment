using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IApplicationRepository : IRepository<Application>
    {
        Task<IEnumerable<Application>> GetApplicationsByUserAsync(Guid userId);
        Task<IEnumerable<Application>> GetApplicationsByStatusAsync(ApplicationStatus status);
        Task<IEnumerable<Application>> GetPendingPaymentApplicationsAsync();
        Task<IEnumerable<Application>> GetApprovedApplicationsAsync();
        Task<IEnumerable<Application>> GetRejectedApplicationsAsync();
        Task<Application?> GetApplicationWithPaymentsAsync(Guid applicationId);
        Task<Application?> GetApplicationWithChildAsync(Guid applicationId);
        Task<Application?> GetApplicationWithAllDetailsAsync(Guid applicationId);
        Task<int> GetApplicationCountByStatusAsync(ApplicationStatus status);
        Task<bool> HasPendingApplicationAsync(Guid userId);
    }
}
