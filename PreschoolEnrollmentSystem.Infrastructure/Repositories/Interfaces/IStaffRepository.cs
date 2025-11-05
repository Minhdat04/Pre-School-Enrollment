using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IStaffRepository : IRepository<Staff>
    {
        Task<Staff?> GetByEmailAsync(string email);
        Task<Staff?> GetByFirebaseUidAsync(string firebaseUid);
        Task<Staff?> GetByEmployeeIdAsync(string employeeId);
        Task<IEnumerable<Staff>> GetActiveStaffAsync();
        Task<IEnumerable<Staff>> GetByRoleAsync(UserRole role);
        Task<IEnumerable<Staff>> GetByDepartmentAsync(string department);
        Task<IEnumerable<Staff>> GetByEmploymentStatusAsync(string status);
        Task<IEnumerable<Staff>> GetEligibleStaffAsync();
        Task<IEnumerable<Staff>> GetExpiringBackgroundChecksAsync(int daysThreshold = 30);
        Task<IEnumerable<Staff>> GetExpiredBackgroundChecksAsync();
        Task<IEnumerable<Staff>> GetPendingBackgroundChecksAsync();
        Task<IEnumerable<Staff>> SearchByNameAsync(string searchTerm);
        Task<IEnumerable<Staff>> GetByJobTitleAsync(string jobTitle);
        Task<IEnumerable<Staff>> GetRecentlyHiredAsync(int days = 30);
        Task<IEnumerable<Staff>> GetByTenureAsync(int minimumYears);
        Task<(int TotalStaff, int ActiveStaff, int AdminCount, int ExpiringChecks, int PendingChecks)> GetStatisticsAsync();
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmployeeIdExistsAsync(string employeeId);
        // Task<Staff?> GetWithClassesAsync(Guid staffId);
        Task<IEnumerable<Staff>> GetAvailableForAssignmentAsync();
    }
}