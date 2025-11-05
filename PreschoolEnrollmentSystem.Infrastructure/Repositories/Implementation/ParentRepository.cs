using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation
{
    public class ParentRepository : Repository<Parent>, IParentRepository
    {
        public ParentRepository(ApplicationDbContext context) : base(context)
        {
        }

        #region Basic Lookups

        public async Task<Parent?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            // Normalize email to lowercase for case-insensitive comparison
            var normalizedEmail = email.ToLowerInvariant();

            return await _dbSet
                .Where(p => !p.IsDeleted) // Soft delete filter
                .FirstOrDefaultAsync(p => p.Email.ToLower() == normalizedEmail);
        }
        public async Task<Parent?> GetByFirebaseUidAsync(string firebaseUid)
        {
            if (string.IsNullOrWhiteSpace(firebaseUid))
            {
                throw new ArgumentException("Firebase UID cannot be null or empty", nameof(firebaseUid));
            }

            return await _dbSet
                .Where(p => !p.IsDeleted) // Soft delete filter
                .FirstOrDefaultAsync(p => p.FirebaseUid == firebaseUid);
        }
        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            var normalizedEmail = email.ToLowerInvariant();

            return await _dbSet
                .Where(p => !p.IsDeleted)
                .AnyAsync(p => p.Email.ToLower() == normalizedEmail);
        }

        #endregion

        #region Filtered Lists

        public async Task<IEnumerable<Parent>> GetActiveParentsAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetIncompleteProfilesAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.IsActive && p.ProfileCompletionPercentage < 100)
                .OrderBy(p => p.ProfileCompletionPercentage) // Lowest completion first
                .ThenBy(p => p.CreatedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetEnrollmentEligibleParentsAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted
                    && p.IsActive
                    && p.EmailVerified
                    && p.AcceptedTerms
                    && !string.IsNullOrEmpty(p.EmergencyContactName)
                    && !string.IsNullOrEmpty(p.EmergencyContactPhone))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetUnverifiedEmailParentsAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.IsActive && !p.EmailVerified)
                .OrderBy(p => p.CreatedAt) // Oldest registrations first
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetRecentlyRegisteredAsync(int days = 7)
        {
            if (days < 1)
            {
                throw new ArgumentException("Days must be greater than 0", nameof(days));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _dbSet
                .Where(p => !p.IsDeleted && p.CreatedAt >= cutoffDate)
                .OrderByDescending(p => p.CreatedAt) // Newest first
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetInactiveParentsAsync(int daysSinceLastLogin = 90)
        {
            if (daysSinceLastLogin < 1)
            {
                throw new ArgumentException("Days must be greater than 0", nameof(daysSinceLastLogin));
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastLogin);

            return await _dbSet
                .Where(p => !p.IsDeleted
                    && p.IsActive
                    && (p.LastLoginAt == null || p.LastLoginAt < cutoffDate))
                .OrderBy(p => p.LastLoginAt ?? p.CreatedAt) // Longest inactive first
                .ToListAsync();
        }

        #endregion

        #region Search

        public async Task<IEnumerable<Parent>> SearchByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Enumerable.Empty<Parent>();
            }

            var normalizedSearch = searchTerm.ToLowerInvariant().Trim();

            return await _dbSet
                .Where(p => !p.IsDeleted
                    && (p.FirstName.ToLower().Contains(normalizedSearch)
                        || p.LastName.ToLower().Contains(normalizedSearch)
                        || (p.FirstName + " " + p.LastName).ToLower().Contains(normalizedSearch)))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Take(50) // Limit results for performance
                .ToListAsync();
        }
        public async Task<IEnumerable<Parent>> GetByPhoneNumberAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty", nameof(phoneNumber));
            }

            // Remove common formatting characters for search
            var cleanPhone = phoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");

            return await _dbSet
                .Where(p => !p.IsDeleted
                    && p.PhoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "").Contains(cleanPhone))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
        }

        #endregion

        #region Statistics

        public async Task<(int TotalParents, int ActiveParents, int UnverifiedEmails, int IncompleteProfiles)> GetStatisticsAsync()
        {
            var totalTask = _dbSet
                .Where(p => !p.IsDeleted)
                .CountAsync();

            var activeTask = _dbSet
                .Where(p => !p.IsDeleted && p.IsActive)
                .CountAsync();

            var unverifiedTask = _dbSet
                .Where(p => !p.IsDeleted && p.IsActive && !p.EmailVerified)
                .CountAsync();

            var incompleteTask = _dbSet
                .Where(p => !p.IsDeleted && p.IsActive && p.ProfileCompletionPercentage < 100)
                .CountAsync();

            // Execute all queries in parallel
            await Task.WhenAll(totalTask, activeTask, unverifiedTask, incompleteTask);

            return (
                TotalParents: await totalTask,
                ActiveParents: await activeTask,
                UnverifiedEmails: await unverifiedTask,
                IncompleteProfiles: await incompleteTask
            );
        }

        #endregion

        #region Eager Loading (Uncomment when relationships are ready)

        // public async Task<Parent?> GetWithStudentsAsync(Guid parentId)
        // {
        //     return await _dbSet
        //         .Where(p => !p.IsDeleted)
        //         .Include(p => p.Students.Where(s => !s.IsDeleted))
        //         .FirstOrDefaultAsync(p => p.Id == parentId);
        // }

        #endregion

        #region Custom Business Logic

        public async Task UpdateProfileCompletionAsync(Guid parentId)
        {
            var parent = await GetByIdAsync(parentId);

            if (parent != null)
            {
                parent.CalculateProfileCompletion();
                Update(parent);
                await SaveChangesAsync();
            }
        }
        public async Task MarkEmailVerifiedAsync(string firebaseUid)
        {
            var parent = await GetByFirebaseUidAsync(firebaseUid);

            if (parent != null && !parent.EmailVerified)
            {
                parent.EmailVerified = true;
                parent.UpdatedAt = DateTime.UtcNow;
                Update(parent);
                await SaveChangesAsync();
            }
        }
        public async Task UpdateLastLoginAsync(string firebaseUid)
        {
            var parent = await GetByFirebaseUidAsync(firebaseUid);

            if (parent != null)
            {
                parent.UpdateLastLogin();
                Update(parent);
                await SaveChangesAsync();
            }
        }

        #endregion
    }
}