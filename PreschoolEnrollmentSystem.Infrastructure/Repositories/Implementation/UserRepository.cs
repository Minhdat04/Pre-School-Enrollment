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
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted && u.Role == role)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetParentsAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Parent);
        }

        public async Task<IEnumerable<User>> GetStaffAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Staff);
        }

        public async Task<IEnumerable<User>> GetTeachersAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Teacher);
        }

        public async Task<IEnumerable<User>> GetAdminsAsync()
        {
            return await GetUsersByRoleAsync(UserRole.Admin);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _dbSet
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserWithChildrenAsync(Guid userId)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithApplicationsAsync(Guid userId)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .Include(u => u.Applications.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithStudentsAsync(Guid userId)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .Include(u => u.Students.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithPaymentsAsync(Guid userId)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .Include(u => u.Payments.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserByIdWithAllDetailsAsync(Guid userId)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted)
                .Include(u => u.Children.Where(c => !c.IsDeleted))
                .Include(u => u.Applications.Where(a => !a.IsDeleted))
                .Include(u => u.Students.Where(s => !s.IsDeleted))
                .Include(u => u.Payments.Where(p => !p.IsDeleted))
                .Include(u => u.Classroom)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IEnumerable<User>> GetUsersByStatusAsync(UserStatus status)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted && u.Status == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await GetUsersByStatusAsync(UserStatus.Active);
        }

        public async Task<IEnumerable<User>> GetInactiveUsersAsync()
        {
            return await GetUsersByStatusAsync(UserStatus.Inactive);
        }

        public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var query = _dbSet.Where(u => !u.IsDeleted && u.Email.ToLower() == email.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<int> GetUserCountByRoleAsync(UserRole role)
        {
            return await _dbSet
                .Where(u => !u.IsDeleted && u.Role == role)
                .CountAsync();
        }
    }
}
