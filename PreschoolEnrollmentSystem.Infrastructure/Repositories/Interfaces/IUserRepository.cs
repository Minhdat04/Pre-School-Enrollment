using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Core.Enums;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);
        Task<IEnumerable<User>> GetParentsAsync();
        Task<IEnumerable<User>> GetStaffAsync();
        Task<IEnumerable<User>> GetTeachersAsync();
        Task<IEnumerable<User>> GetAdminsAsync();
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserWithChildrenAsync(Guid userId);
        Task<User?> GetUserWithApplicationsAsync(Guid userId);
        Task<User?> GetUserWithStudentsAsync(Guid userId);
        Task<User?> GetUserWithPaymentsAsync(Guid userId);
        Task<User?> GetUserByIdWithAllDetailsAsync(Guid userId);
        Task<IEnumerable<User>> GetUsersByStatusAsync(UserStatus status);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetInactiveUsersAsync();
        Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);
        Task<int> GetUserCountByRoleAsync(UserRole role);
    }
}
