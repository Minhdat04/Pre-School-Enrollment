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
    public class StaffRepository : Repository<Staff>, IStaffRepository
    {
        public StaffRepository(ApplicationDbContext context) : base(context)
        {
        }

        #region Basic Lookups

        public async Task<Staff?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            var normalizedEmail = email.ToLowerInvariant();

            return await _dbSet
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);
        }
        public async Task<Staff?> GetByFirebaseUidAsync(string firebaseUid)
        {
            if (string.IsNullOrWhiteSpace(firebaseUid))
            {
                throw new ArgumentException("Firebase UID cannot be null or empty", nameof(firebaseUid));
            }

            return await _dbSet
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync(s => s.FirebaseUid == firebaseUid);
        }
        public async Task<Staff?> GetByEmployeeIdAsync(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                throw new ArgumentException("Employee ID cannot be null or empty", nameof(employeeId));
            }

            return await _dbSet
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId);
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            var normalizedEmail = email.ToLowerInvariant();

            return await _dbSet
                .Where(s => !s.IsDeleted)
                .AnyAsync(s => s.Email.ToLower() == normalizedEmail);
        }
        public async Task<bool> EmployeeIdExistsAsync(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                return false;
            }

            return await _dbSet
                .Where(s => !s.IsDeleted)
                .AnyAsync(s => s.EmployeeId == employeeId);
        }

        #endregion

        #region Filtered Lists

        public async Task<IEnumerable<Staff>> GetActiveStaffAsync()
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.IsActive)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.Role == role)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetByDepartmentAsync(string department)
        {
            if (string.IsNullOrWhiteSpace(department))
            {
                return Enumerable.Empty<Staff>();
            }

            var normalizedDept = department.ToLowerInvariant();

            return await _dbSet
                .Where(s => !s.IsDeleted
                    && s.Department != null
                    && s.Department.ToLower() == normalizedDept)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetByEmploymentStatusAsync(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Enumerable.Empty<Staff>();
            }

            var normalizedStatus = status.ToLowerInvariant();

            return await _dbSet
                .Where(s => !s.IsDeleted && s.EmploymentStatus.ToLower() == normalizedStatus)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetByJobTitleAsync(string jobTitle)
        {
            if (string.IsNullOrWhiteSpace(jobTitle))
            {
                return Enumerable.Empty<Staff>();
            }

            var normalizedTitle = jobTitle.ToLowerInvariant();

            return await _dbSet
                .Where(s => !s.IsDeleted && s.JobTitle.ToLower() == normalizedTitle)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        #endregion

        #region Background Check Queries

        public async Task<IEnumerable<Staff>> GetEligibleStaffAsync()
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(s => !s.IsDeleted
                    && s.IsActive
                    && s.EmploymentStatus == "Active"
                    && s.BackgroundCheckStatus == "Approved"
                    && (s.BackgroundCheckExpiresAt == null || s.BackgroundCheckExpiresAt > now))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetExpiringBackgroundChecksAsync(int daysThreshold = 30)
        {
            if (daysThreshold < 1)
            {
                throw new ArgumentException("Days threshold must be greater than 0", nameof(daysThreshold));
            }

            var now = DateTime.UtcNow;
            var thresholdDate = now.AddDays(daysThreshold);

            return await _dbSet
                .Where(s => !s.IsDeleted
                    && s.IsActive
                    && s.BackgroundCheckExpiresAt != null
                    && s.BackgroundCheckExpiresAt > now
                    && s.BackgroundCheckExpiresAt <= thresholdDate)
                .OrderBy(s => s.BackgroundCheckExpiresAt) // Soonest first
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetExpiredBackgroundChecksAsync()
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Where(s => !s.IsDeleted
                    && s.BackgroundCheckExpiresAt != null
                    && s.BackgroundCheckExpiresAt <= now)
                .OrderBy(s => s.BackgroundCheckExpiresAt) // Most overdue first
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetPendingBackgroundChecksAsync()
        {
            return await _dbSet
                .Where(s => !s.IsDeleted && s.BackgroundCheckStatus == "Pending")
                .OrderBy(s => s.HireDate) // Oldest hires first
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetAvailableForAssignmentAsync()
        {
            return await GetEligibleStaffAsync();
        }

        #endregion

        #region Time-Based Queries

        public async Task<IEnumerable<Staff>> GetRecentlyHiredAsync(int days = 30)
        {
            if (days < 1)
            {
                throw new ArgumentException("Days must be greater than 0", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _dbSet
                .Where(s => !s.IsDeleted && s.HireDate >= cutoffDate)
                .OrderByDescending(s => s.HireDate) // Newest first
                .ToListAsync();
        }
        public async Task<IEnumerable<Staff>> GetByTenureAsync(int minimumYears)
        {
            if (minimumYears < 0)
            {
                throw new ArgumentException("Minimum years cannot be negative", nameof(minimumYears));
            }

            var cutoffDate = DateTime.UtcNow.AddYears(-minimumYears);

            return await _dbSet
                .Where(s => !s.IsDeleted && s.HireDate <= cutoffDate)
                .OrderBy(s => s.HireDate) // Longest tenure first
                .ToListAsync();
        }

        #endregion

        #region Search

        public async Task<IEnumerable<Staff>> SearchByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Enumerable.Empty<Staff>();
            }

            var normalizedSearch = searchTerm.ToLowerInvariant().Trim();

            return await _dbSet
                .Where(s => !s.IsDeleted
                    && (s.FirstName.ToLower().Contains(normalizedSearch)
                        || s.LastName.ToLower().Contains(normalizedSearch)
                        || (s.FirstName + " " + s.LastName).ToLower().Contains(normalizedSearch)
                        || (s.EmployeeId != null && s.EmployeeId.ToLower().Contains(normalizedSearch))))
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Take(50) // Limit results for performance
                .ToListAsync();
        }

        #endregion

        #region Statistics

        public async Task<(int TotalStaff, int ActiveStaff, int AdminCount, int ExpiringChecks, int PendingChecks)> GetStatisticsAsync()
        {
            var totalTask = _dbSet
                .Where(s => !s.IsDeleted)
                .CountAsync();

            var activeTask = _dbSet
                .Where(s => !s.IsDeleted && s.IsActive)
                .CountAsync();

            var adminTask = _dbSet
                .Where(s => !s.IsDeleted && s.Role == UserRole.Admin)
                .CountAsync();

            var now = DateTime.UtcNow;
            var thresholdDate = now.AddDays(30);

            var expiringTask = _dbSet
                .Where(s => !s.IsDeleted
                    && s.IsActive
                    && s.BackgroundCheckExpiresAt != null
                    && s.BackgroundCheckExpiresAt > now
                    && s.BackgroundCheckExpiresAt <= thresholdDate)
                .CountAsync();

            var pendingTask = _dbSet
                .Where(s => !s.IsDeleted && s.BackgroundCheckStatus == "Pending")
                .CountAsync();

            // Execute all queries in parallel
            await Task.WhenAll(totalTask, activeTask, adminTask, expiringTask, pendingTask);

            return (
                TotalStaff: await totalTask,
                ActiveStaff: await activeTask,
                AdminCount: await adminTask,
                ExpiringChecks: await expiringTask,
                PendingChecks: await pendingTask
            );
        }

        #endregion

        #region Eager Loading (Uncomment when relationships are ready)

        // public async Task<Staff?> GetWithClassesAsync(Guid staffId)
        // {
        //     return await _dbSet
        //         .Where(s => !s.IsDeleted)
        //         .Include(s => s.AssignedClasses.Where(c => !c.IsDeleted))
        //         .FirstOrDefaultAsync(s => s.Id == staffId);
        // }

        #endregion

        #region Custom Business Logic

        public async Task MarkEmailVerifiedAsync(string firebaseUid)
        {
            var staff = await GetByFirebaseUidAsync(firebaseUid);

            if (staff != null && !staff.EmailVerified)
            {
                staff.EmailVerified = true;
                staff.UpdatedAt = DateTime.UtcNow;
                Update(staff);
                await SaveChangesAsync();
            }
        }
        public async Task UpdateLastLoginAsync(string firebaseUid)
        {
            var staff = await GetByFirebaseUidAsync(firebaseUid);

            if (staff != null)
            {
                staff.UpdateLastLogin();
                Update(staff);
                await SaveChangesAsync();
            }
        }
        public async Task UpdateBackgroundCheckAsync(
            Guid staffId,
            string status,
            DateTime? checkDate = null,
            DateTime? expiresAt = null)
        {
            var staff = await GetByIdAsync(staffId);

            if (staff != null)
            {
                staff.BackgroundCheckStatus = status;
                staff.BackgroundCheckDate = checkDate ?? DateTime.UtcNow;
                staff.BackgroundCheckExpiresAt = expiresAt;
                staff.UpdatedAt = DateTime.UtcNow;

                Update(staff);
                await SaveChangesAsync();
            }
        }

        #endregion
    }
}